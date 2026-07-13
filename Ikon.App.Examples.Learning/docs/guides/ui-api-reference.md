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
  // ReactiveBusyExtensions.RunAsync collapses it to:
  // await _busy.RunAsync(_status, LoadAsync);
  // For the busy flag alone (no status reactive), use _busy.AsToken() from Ikon.Common.Core.Reactive instead.
  static class ReactiveBusyExtensions
    // Runs work with busy raised: clears status, sets the flag for the duration of the work (via ReactiveBoolExtensions.AsToken, so it always returns to false), and routes a failure's message into status instead of throwing. Returns whether the work completed, so callers can add their own failure handling on top:
    // if (!await _busy.RunAsync(_status, RefreshAsync))
    // {
    //     _entries.Value = [];
    // }
    static Task<bool> RunAsync(this Reactive<bool> busy, Reactive<string?> status, Func<Task> work)
  // Per-client theme state created by UI.UseTheme. Holds each client's active theme and switches it: ThemeControl.Current is bindable in views, and ThemeControl.ToggleAsync can be bound directly to a button's onClick.
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
    // When true (the default), a re-render reuses the cached output of any subtree whose tracked reactive dependencies are unchanged, skipping re-execution of its content lambda — so an update costs O(changed subtree) rather than O(whole tree). Wire output is identical to the uncached path (proven by the differential oracle in Ikon.Parallax.Test); the only behavioural change is that a subtree reading NON-reactive data no longer refreshes until one of its reactive dependencies changes, which is the intended reactive contract. Set false to force a full re-render every cycle.
    bool EnableSubtreeCaching { get; set; }
    // Defines the root UI view tree. Call this in a reactive context to re-render when dependencies change.
    void Root(string[]? style = null, Action<UIView>? content = null, string? styleId = null)
    // One-call per-client theme handling: syncs each joining client's theme, exposes the active theme as a bindable ThemeControl.Current, and provides dark/light switching. The verbose form
    // private readonly ClientReactive<Theme> _theme = new(Theme.Dark);
    //
    // // In Main:
    // app.ClientJoinedAsync += async args =>
    // {
    //     if (string.IsNullOrEmpty(args.ClientContext.Theme))
    //     {
    //         _theme.Value = Theme.Dark;
    //         await ClientFunctions.SetThemeAsync(Theme.Dark);
    //     }
    //     else
    //     {
    //         _theme.Value = args.ClientContext.IsDarkTheme() ? Theme.Dark : Theme.Light;
    //     }
    // };
    //
    // private async Task ToggleThemeAsync()
    // {
    //     var next = _theme.Value == Theme.Dark ? Theme.Light : Theme.Dark;
    //     await ClientFunctions.SetThemeAsync(next);
    //     _theme.Value = next;
    // }
    // becomes
    // _theme = UI.UseTheme();
    // with _theme.Current bindable in views (e.g. a sun/moon icon) and _theme.ToggleAsync bindable to a button's onClick. When followClient is true (the default), a joining client that already has a theme keeps it and clients without one get defaultTheme; when false, every joining client is forced to defaultTheme. Call once in Main, before clients join.
    ThemeControl UseTheme(Theme defaultTheme = Dark, bool followClient = true)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // True when this render is capturing the build-time boot snapshot (the client's Context.IsSnapshot is set). The snapshot is a public asset shown to everyone before the live UI connects, so gate per-user or sensitive content on this — typically via the SnapshotReveal / SnapshotHide / SnapshotOnly wrappers rather than reading this directly. Always false on the normal live render path.
    bool IsSnapshot { get; }
    // Adds a child node with the given type and props. The props parameter is the non-generic IDictionary on purpose: it's the ONLY type that cleanly accepts BOTH a `Dictionary<string, object>` (the natural non-null shape a model builds) AND a `Dictionary<string, object?>` (props that carry null values) with no nullability warning and no suppression. A generic `Dictionary<string, object?>` param warns CS8620 on the non-null form (identity-modulo-nullability), and no PAIR of generic overloads works either — nullability annotations are erased for overload resolution, so two such overloads are CS0111 (same signature) or CS0121 (ambiguous).
    void AddNode(string type, IDictionary? props = null, List<UIViewNode>? children = null, string? key = null, string[]? style = null, string? styleId = null)
    // Registers a callback as a UI action and returns its ID for use in component props.
    string? CreateAction<T>(Func<ActionArgs<T>, Task>? callback)
    // Registers binary data as a payload and returns a reference string for use as an image src.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Creates a new view node with the given type, props, and optional children.
    ctor(string type, Guid viewId, Dictionary<string, object?> props, List<UIViewNode>? children = null, string? key = null, IReadOnlyList<string>? styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>>? styleIdProps = null)
    // Ordered child nodes.
    List<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string? ContentFingerprint { get; }
    // True when UIViewNode.StableHint came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of UIViewNode.Id for fast lookups.
    int IdHash { get; }
    // When true, nodes include source file and line markers for debugging.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer.
    Dictionary<string, object?> Props { get; }
    // Source file and line marker for debugging, included only when UIViewNode.IncludeSourceMarkers is true.
    string? SourceMarker { get; }
    // Hint string used by the stable ID generator to produce deterministic IDs.
    string? StableHint { get; }
    // Resolved Crosswind style class identifiers.
    IReadOnlyList<string> StyleIds { get; }
    // The component type name (e.g. "div", "button").
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  // Configuration for a chart axis including legend text, tick appearance, and label truncation.
  sealed class AxisConfig : IEquatable<AxisConfig>
    ctor()
    // Format string for tick labels. For time scales, use d3-time-format tokens (e.g. "%H:%M", "%m/%d %H:%M").
    string? Format { get; init; }
    string? Legend { get; init; }
    int? LegendOffset { get; init; }
    int? TickPadding { get; init; }
    int? TickRotation { get; init; }
    int? TickSize { get; init; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickValues { get; init; }
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
  class ChartAxisStyle : IEquatable<ChartAxisStyle>
    ctor()
    string? DomainColor { get; init; }
    ChartTextStyle? Legend { get; init; }
    string? TickColor { get; init; }
    ChartTextStyle? TickLabel { get; init; }
  // Event arguments for chart click interactions.
  sealed class ChartClickArgs : IEquatable<ChartClickArgs>
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
  class ChartCrosshairStyle : IEquatable<ChartCrosshairStyle>
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Renders an interactive bar chart with configurable grouping, layout, axes, and theming. Pass valueUnit to render tooltip values and value-axis ticks in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void BarChart(this UIView view, string[]? style = null, IEnumerable<Dictionary<string, object>>? data = null, IEnumerable<string>? keys = null, string? indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string? labelTextColor = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderRadius = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Renders an interactive line chart with configurable curves, points, areas, and crosshairs. Pass valueUnit to render tooltip Y values and left-axis ticks in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void LineChart(this UIView view, string[]? style = null, IEnumerable<LineChartSeries>? data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? xScaleMin = null, double? xScaleMax = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin? margin = null, AxisConfig? axisTop = null, AxisConfig? axisRight = null, AxisConfig? axisBottom = null, AxisConfig? axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string? pointColor = null, string? pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, bool? enableSlices = null, string? xFormat = null, string? yFormat = null, string? valueUnit = null, string? gradientFromColor = null, string? gradientToColor = null, IEnumerable<double>? gridXValues = null, IEnumerable<double>? gridYValues = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
    // Renders an interactive pie/donut chart with configurable arc labels, link labels, and legends. Pass valueUnit to render tooltip values in a human-scaled unit — well-known units are "milliseconds", "seconds", "bytes", "percent", and "usd" (e.g. 1333.9 milliseconds renders as "1.33 s"); any other string is appended as a plain suffix.
    static void PieChart(this UIView view, string[]? style = null, IEnumerable<PieChartDatum>? data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin? margin = null, bool? enableArcLabels = null, string? arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string? arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string? arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig>? legends = null, IEnumerable<string>? colors = null, ChartColorScheme? colorScheme = null, ChartTheme? theme = null, string? borderColor = null, double? borderWidth = null, string? valueFormat = null, string? valueUnit = null, bool? arcLabelAsPercentage = null, bool? isInteractive = null, Func<ChartClickArgs, Task>? onClick = null, string? styleId = null, string? key = null)
  // Styling for chart grid lines.
  class ChartGridStyle : IEquatable<ChartGridStyle>
    ctor()
    string? LineColor { get; init; }
    string? LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  class ChartLabelsStyle : IEquatable<ChartLabelsStyle>
    ctor()
    ChartTextStyle? Text { get; init; }
  // Styling for chart legend text and title.
  class ChartLegendStyle : IEquatable<ChartLegendStyle>
    ctor()
    ChartTextStyle? Text { get; init; }
    ChartTextStyle? Title { get; init; }
  // Margin configuration for chart containers.
  sealed class ChartMargin : IEquatable<ChartMargin>
    ctor()
    int? Bottom { get; init; }
    int? Left { get; init; }
    int? Right { get; init; }
    int? Top { get; init; }
  // Text styling for chart elements.
  class ChartTextStyle : IEquatable<ChartTextStyle>
    ctor()
    string? Color { get; init; }
    string? FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
  class ChartTheme : IEquatable<ChartTheme>
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
  class ChartTooltipStyle : IEquatable<ChartTooltipStyle>
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
  sealed class LegendConfig : IEquatable<LegendConfig>
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
  sealed class LineChartPoint : IEquatable<LineChartPoint>
    ctor()
    // X value — a string label for point scales, or a number for linear/time scales, so the type is genuinely mixed.
    required object X { get; init; }
    required double Y { get; init; }
  // A named data series for a line chart, containing an ordered collection of points.
  sealed class LineChartSeries : IEquatable<LineChartSeries>
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
  sealed class PieChartDatum : IEquatable<PieChartDatum>
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
  class Cell : IEquatable<Cell>
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
    // Creates a badge cell. The tone renders through the themed badge recipe (Theming.Badge.*); style classes merge on top of the tone token, and the literal "unstyled" class opts out of the tone token entirely.
    static Cell Badge(string value, SemanticTone? tone = null, string[]? style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool value, string actionId, string[]? style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string? value, string[]? style = null)
  // An action button that can be displayed within a data table cell.
  class CellAction : IEquatable<CellAction>
    ctor(string Label, string ActionId, string[]? Style = null, string? Icon = null)
    string ActionId { get; init; }
    string? Icon { get; init; }
    string Label { get; init; }
    string[]? Style { get; init; }
  // The kind of content a data table cell renders.
  enum CellType
    Text
    Badge
    Action
    Actions
    Checkbox
  // Defines a column in a data table including header text, width, and alignment.
  class DataTableColumn : IEquatable<DataTableColumn>
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
  class DataTableRow : IEquatable<DataTableRow>
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }
  // Per-slot style overrides for DataTableExtensions.DataTable. Each slot is a Crosswind class array that merges on top of the slot's themed default, exactly like a component's style: parameter. Set only the slots you are changing:
  // view.DataTable(columns, rows, totalCount, pageIndex, pageSize,
  //     styles: new DataTableStyles { Header = ["bg-muted"], Row = ["hover:bg-accent"] });
  sealed class DataTableStyles : IEquatable<DataTableStyles>
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
    // Canvas for editing images with brush and eraser tools.
    static void ImageEditorCanvas(this UIView view, string[]? style = null, string? src = null, int? brushWidth = null, string? brushColor = null, ImageEditorTool? tool = null, double? zoom = null, bool? highResolution = null, int? textMaxLength = null, int? textFontSize = null, int? textPadding = null, Func<ImageEditorSaveArgs, Task>? onSave = null, Func<ImageEditorHistoryArgs, Task>? onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string? styleId = null, string? key = null)
  // Event args for when the undo/redo history state changes.
  sealed class ImageEditorHistoryArgs : IEquatable<ImageEditorHistoryArgs>
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed class ImageEditorSaveArgs : IEquatable<ImageEditorSaveArgs>
    ctor(string ImageData)
    string ImageData { get; init; }
  // Drawing tool active on an ImageEditorCanvas.
  enum ImageEditorTool
    Brush
    Eraser
    Text
    Arrow
    Region

namespace Ikon.Parallax.Components.Rive
  // Layout alignment options for Rive animations.
  enum RiveAlignment
    Center
    TopLeft
    TopCenter
    TopRight
    CenterLeft
    CenterRight
    BottomLeft
    BottomCenter
    BottomRight
  // Represents a color value for Rive animations.
  sealed class RiveColor
    ctor()
    // Blue channel (0-255).
    int B { get; init; }
    // Green channel (0-255).
    int G { get; init; }
    // Red channel (0-255).
    int R { get; init; }
  // Data received from a Rive event.
  sealed class RiveEventData
    ctor()
    // Delay in seconds before the event fires.
    double? Delay { get; init; }
    // The name of the Rive event.
    string Name { get; init; }
    // Custom properties attached to the event as JSON elements.
    Dictionary<string, JsonElement>? Properties { get; init; }
    // Type-safe accessor for the event's custom properties.
    RiveEventProperties Props { get; }
    // Target identifier for the event.
    string? Target { get; init; }
    // The Rive event type identifier.
    int? Type { get; init; }
    // URL associated with the event, if any.
    string? Url { get; init; }
  // Helper class for accessing Rive event properties with type-safe methods.
  sealed class RiveEventProperties
    ctor(Dictionary<string, JsonElement>? properties)
    // Gets a boolean property value, or defaultValue if not found.
    bool GetBool(string key, bool defaultValue = false)
    // Gets a double property value, or defaultValue if not found.
    double GetDouble(string key, double defaultValue = 0)
    // Gets an integer property value, or defaultValue if not found.
    int GetInt(string key, int defaultValue = 0)
    // Gets a string property value, or defaultValue if not found.
    string GetString(string key, string defaultValue = "")
  // Extension methods for Rive animation components.
  static class RiveExtensions
    // Canvas for rendering Rive animations with state machine support.
    static void RiveCanvas(this UIView view, string[]? style = null, string? source = null, IEnumerable<string>? stateMachines = null, RiveViewModel? viewModel = null, IEnumerable<RiveTrigger>? triggers = null, Func<RiveEventData, Task>? onEvent = null, RiveFit? layoutFit = null, RiveAlignment? layoutAlignment = null, bool? autoplay = null, bool? useOffscreenRenderer = null, bool? autoBind = null, bool? enableMultiTouch = null, bool? dispatchPointerExit = null, bool? isTouchScrollEnabled = null, bool? shouldDisableRiveListeners = null, IEnumerable<RiveKeyboardBinding>? keyboardBindings = null, string? backgroundColor = null, string? width = null, string? height = null, string? styleId = null, string? key = null)
  // Layout fit options for Rive animations.
  enum RiveFit
    Contain
    Cover
    Fill
    FitWidth
    FitHeight
    None
    ScaleDown
    Layout
  // Static helpers for creating keyboard bindings.
  static class RiveKeyboard
    // Creates a boolean binding that sets a Rive boolean input to true while the key is held.
    static RiveKeyboardBinding Boolean(RiveKeyboardKey key, string inputName)
    // Creates a trigger binding that fires a Rive trigger input when the key is pressed.
    static RiveKeyboardBinding Trigger(RiveKeyboardKey key, string inputName)
  // Represents a keyboard binding for a Rive animation input.
  sealed class RiveKeyboardBinding
    ctor()
    // The Rive state machine input name to bind to.
    string InputName { get; init; }
    // The keyboard key that triggers this binding.
    RiveKeyboardKey Key { get; init; }
    // Whether this binding is a boolean (held) or trigger (pressed) type.
    RiveKeyboardBindingKind Kind { get; init; }
  // Types of keyboard bindings for Rive inputs.
  enum RiveKeyboardBindingKind
    Boolean
    Trigger
  // Keyboard keys that can be bound to Rive inputs.
  enum RiveKeyboardKey
    ArrowUp
    ArrowDown
    ArrowLeft
    ArrowRight
  // Represents a trigger that can be fired in a Rive animation. Calling Fire() increments the sequence and triggers a UI re-render.
  sealed class RiveTrigger
    ctor(string name)
    // The name of this trigger, matching the Rive input name.
    string Name { get; }
    // Current trigger sequence number, incremented on each fire.
    long Sequence { get; }
    // Fires the trigger, causing the Rive animation to respond on the next render.
    void Fire()
  // Fluent builder for constructing Rive view model data.
  sealed class RiveViewModel
    ctor()
    // Sets a boolean input on the Rive state machine.
    RiveViewModel Boolean(string name, bool? value)
    // Sets an RGB color input on the Rive state machine.
    RiveViewModel Color(string name, int r, int g, int b)
    // Sets an enum input on the Rive state machine by integer value.
    RiveViewModel Enum(string name, int? value)
    // Sets a number input on the Rive state machine.
    RiveViewModel Number(string name, double? value)
    // Sets a string input on the Rive state machine.
    RiveViewModel String(string name, string? value)

namespace Ikon.Parallax.Components.Standard
  // Extension methods for accessibility components.
  static class AccessibilityExtensions
    // Wraps an icon with accessible label for screen readers.
    static void AccessibleIcon(this UIView view, string[]? style = null, string? label = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Hides content visually while keeping it accessible to screen readers.
    static void VisuallyHidden(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  class ActionEvent : IEquatable<ActionEvent>
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // Types of client-side actions that can be triggered from the server.
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
  // Base class for client-side action configuration.
  abstract class ActionOptions : IEquatable<ActionOptions>
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Inline alert banner composite over the theme's Alert token recipe.
  static class AlertExtensions
    // Inline alert banner — tone-tinted surface with an icon, title, optional description, and an optional dismiss button. The icon defaults per tone (success check, warning triangle, error alert, info circle). Caller styles merge on top of the tone's Theming.Alert token; include the literal "unstyled" class to opt out.
    static void Alert(this UIView view, string title, SemanticTone tone = Neutral, string[]? style = null, string? description = null, string? icon = null, bool showIcon = true, Func<Task>? onDismiss = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? iconStyle = null, string[]? dismissStyle = null, Action<UIView>? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Status pill composite over the theme's Badge token recipe. Replaces the hand-rolled inline-flex/rounded-full/px-2 pill pattern with a one-liner: view.Badge("Live", SemanticTone.Success).
  static class BadgeExtensions
    // Small status pill. With zero style args it renders the themed pill for the tone (Theming.Badge.*); caller styles merge on top of the base token, and the literal "unstyled" class opts out of the base entirely.
    static void Badge(this UIView view, string text, SemanticTone tone = Neutral, string[]? style = null, BadgeSize size = Md, bool outline = false, bool dot = false, string[]? dotStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Size of a BadgeExtensions.Badge.
  enum BadgeSize
    Sm
    Md
    Lg
  // Breadcrumb trail composite over the theme's Breadcrumb token recipe.
  static class BreadcrumbExtensions
    // Breadcrumb navigation trail. Items with an OnClick render as clickable links; the last item always renders as the non-clickable current page (with aria-current="page"). A chevron separator is placed between items.
    static void Breadcrumb(this UIView view, IReadOnlyList<BreadcrumbItem> items, string[]? style = null, string? separatorIcon = null, string[]? linkStyle = null, string[]? itemStyle = null, string[]? pageStyle = null, string[]? separatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // One entry in a BreadcrumbExtensions.Breadcrumb trail.
  sealed class BreadcrumbItem : IEquatable<BreadcrumbItem>
    ctor(string Label, Func<Task>? OnClick = null)
    // Visible text of the crumb.
    string Label { get; init; }
    // Navigation callback. Ignored for the last item, which always renders as the non-clickable current page.
    Func<Task>? OnClick { get; init; }
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // Month-grid date selector. Renders a single month with day cells. Dates are ISO yyyy-MM-dd strings.
    static void Calendar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? month = null, string? defaultMonth = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, string? locale = null, bool? disabled = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onMonthChange = null)
    // Button that opens a popover containing a CalendarExtensions.Calendar.
    static void DatePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, string? format = null, string? minDate = null, string? maxDate = null, IReadOnlyList<string>? disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? calendarStyle = null, string[]? headerStyle = null, string[]? weekdayStyle = null, string[]? dayStyle = null, string[]? daySelectedStyle = null, string[]? dayTodayStyle = null, string[]? dayOutsideStyle = null, string[]? dayDisabledStyle = null, string[]? navButtonStyle = null, string[]? titleStyle = null, string[]? gridStyle = null, string[]? rowStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Which physical camera to prefer when starting the capture. Maps to the W3C MediaStream facingMode constraint and is treated as an "ideal" hint — the browser falls back to whatever camera is available if the requested side does not exist (e.g. desktops without a rear camera).
  enum CameraFacing
    User
    Environment
  // Options for capturing an image from the client's camera.
  sealed class CaptureImageActionOptions : ActionOptions, IEquatable<CaptureImageActionOptions>
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
  // Hardware constraints for image capture. Applied directly when CaptureImageActionOptions.Mode is CaptureImageMode.Headless. In CaptureImageMode.Native mode only CaptureImageConstraints.FacingMode is honored (mapped to the file input's capture attribute); the OS camera UI ignores other constraints.
  sealed class CaptureImageConstraints : IEquatable<CaptureImageConstraints>
    ctor()
    // Preferred camera device ID. Headless mode only.
    string? DeviceId { get; init; }
    // Preferred camera side (front vs. rear). Most useful on phones where CameraFacing.Environment opens the rear camera by default. On desktops with only a webcam this is ignored.
    CameraFacing? FacingMode { get; init; }
  // How the image capture is presented to the user. Controls whether the OS camera UI is invoked or whether the capture happens silently.
  enum CaptureImageMode
    Native
    Headless
  // Card-family composites: Card, StatCard, and EmptyState. All are server-side compositions over the container/text primitives styled by the Theming.Card / Theming.StatCard / Theming.EmptyState token recipes — beautiful by default, every part overridable.
  static class CardExtensions
    // Surface container with optional header (title + description), body content, and footer, per the theme's Card recipe. With zero style args it renders the themed card (Theming.Card.Default; Theming.Card.Interactive when onClick is set). Caller styles merge on top of the base token; include the literal "unstyled" class to opt out of the base entirely.
    static void Card(this UIView view, string[]? style = null, string? title = null, string? description = null, Action<UIView>? header = null, Action<UIView>? content = null, Action<UIView>? footer = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? contentStyle = null, string[]? footerStyle = null, Delegate? onClick = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Card — positional (style, children) overload so view.Card([style], v => {...}) binds the lambda to the body instead of tripping on the title parameter.
    static void Card(this UIView view, string[]? style, Action<UIView> children)
    // Centered placeholder for empty lists/pages — optional icon, title, optional description, and an optional action row — per the theme's EmptyState recipe.
    static void EmptyState(this UIView view, string title, string[]? style = null, string? description = null, string? icon = null, Action<UIView>? action = null, string[]? iconWrapStyle = null, string[]? iconStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? actionsStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Dashboard statistic card — label, large value, optional delta with trend arrow, and an optional icon box — per the theme's StatCard recipe.
    static void StatCard(this UIView view, string label, string value, string[]? style = null, string? delta = null, StatTrend trend = Flat, string? trendLabel = null, string? icon = null, SemanticTone iconTone = Neutral, string[]? labelStyle = null, string[]? valueStyle = null, string[]? trendStyle = null, string[]? iconBoxStyle = null, string[]? iconStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    Start
    Center
    End
  // Responsive carousel configuration applied above a container-width threshold.
  sealed class CarouselBreakpoint : IEquatable<CarouselBreakpoint>
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
    // Horizontal or vertical carousel with optional navigation arrows and indicator dots.
    // Remarks:
    // Provide slides via slides for the simple case, or via the content builder using CarouselExtensions.Slide for fully custom children.
    static void Carousel(this UIView view, string[]? style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, int? slidesPerView = null, int? slidesPerGroup = null, int? slideGapPx = null, IEnumerable<CarouselBreakpoint>? breakpoints = null, IEnumerable<CarouselSlideItem>? slides = null, bool? showArrows = null, bool? showIndicators = null, string? previousLabel = null, string? nextLabel = null, string? previousIconName = null, string? nextIconName = null, string[]? rootStyle = null, string[]? viewportStyle = null, string[]? slideStyle = null, string[]? previousStyle = null, string[]? nextStyle = null, string[]? indicatorsStyle = null, string[]? indicatorStyle = null, string[]? indicatorActiveStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null, Func<int, Task>? onIndexChange = null)
    // A single slide inside a CarouselExtensions.Carousel. Use when rendering slides manually.
    static void Slide(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Declarative slide definition for CarouselExtensions.Carousel.
  sealed class CarouselSlideItem : IEquatable<CarouselSlideItem>
    ctor(Action<UIView> Content, string? Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string? Key { get; init; }
  // Extension methods for the ChatLog primitive — the canonical chat-bubble layout shape: header + scrolling auto-scrolled body + composer. Wraps ScrollColumnExtensions.ScrollColumn with chat-friendly defaults so callers don't have to remember to set autoScroll: true.
  static class ChatLogExtensions
    // Renders a chat-style scrolling region: an optional pinned header (e.g. "Conversation"), a scrollable body that auto-scrolls to the bottom on change, and an optional pinned footer (typically the input row).
    // Remarks:
    // Use this instead of a manual Column(overflow-auto) for chat, transcript, or any other "newest at the bottom, follow when content grows" layout. Avoids the common bug of new messages landing off-screen because the user has scrolled but the framework has no signal to re-engage auto-scroll. autoScrollKey is what tells the framework when to re-anchor to the bottom — pass the message collection itself (any reactive contributes its change version), a count, or any other value that changes when the content does. Example:
    // view.ChatLog(
    //     ["h-[480px] w-full"],
    //     autoScrollKey: messages,
    //     header: h => h.Text("Conversation"),
    //     content: body =>
    //     {
    //         foreach (var msg in messages) body.Row(...);
    //     },
    //     footer: f => f.TextField(bind: _draft, onSubmit: ...));
    static void ChatLog(this UIView view, string[]? style = null, object? autoScrollKey = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, string? styleId = null, string? key = null)
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    static void CodeEditor(this UIView view, string? value = null, string? defaultValue = null, string? language = null, string? placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? gutterStyle = null, string[]? contentStyle = null, string[]? languageBadgeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Represents collision detection strategy for @dnd-kit.
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  // Output string format for ColorPickerExtensions.ColorPicker.
  enum ColorFormat
    Hex
    Rgb
    Hsl
  // Extension methods for ColorPicker components.
  static class ColorPickerExtensions
    // Swatch-triggered color picker with hue slider, saturation/lightness square, and hex input.
    static void ColorPicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string>? presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onValueCommit = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    Left
    Center
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed class ContactsActionEvent : ActionEvent, IEquatable<ContactsActionEvent>
    ctor(bool Success, IReadOnlyList<ClientContact>? Contacts)
    IReadOnlyList<ClientContact>? Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    static void Box(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(this UIView view, string[]? style, Action<UIView> children)
    // Container with vertical flexbox layout (flex-col).
    static void Column(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Column — positional (style, children) overload.
    static void Column(this UIView view, string[]? style, Action<UIView> children)
    // Container with flexbox layout enabled.
    static void Flex(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Flex — positional (style, children) overload (see ContainerExtensions.Box).
    static void Flex(this UIView view, string[]? style, Action<UIView> children)
    // Container with CSS grid layout enabled.
    static void Grid(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Grid — positional (style, children) overload.
    static void Grid(this UIView view, string[]? style, Action<UIView> children)
    // Absolutely positioned layer within a Stack container.
    static void Layer(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Layer — positional (style, children) overload (see ContainerExtensions.Box).
    static void Layer(this UIView view, string[]? style, Action<UIView> children)
    // Container with horizontal flexbox layout (flex-row).
    static void Row(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Row — positional (style, children) overload (see ContainerExtensions.Box).
    static void Row(this UIView view, string[]? style, Action<UIView> children)
    // Loading spinner — an animated circular indicator for async/pending states. A typed convenience over the spin utility classes (equivalent to a div with the Theming.Icon.Spinner style): render it while waiting on data, e.g. if (_loading.Value) { view.Spinner(); }. Override colour/size via the style array; the default tracks the theme's muted foreground.
    static void Spinner(this UIView view, string[]? style = null, SpinnerSize size = Md, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Container for layering children on top of each other. Use with Layer components as children.
    static void Stack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Stack — positional (style, children) overload (see ContainerExtensions.Box).
    static void Stack(this UIView view, string[]? style, Action<UIView> children)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  class ContentGridColumn : IEquatable<ContentGridColumn>
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
  sealed class CopyToClipboardActionOptions : ActionOptions, IEquatable<CopyToClipboardActionOptions>
    ctor()
    // The text to copy.
    required string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void ActionButton(this UIView view, string[]? style = null, ActionKind action = Unknown, string? text = null, ActionOptions? options = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<ActionEvent, Task>? onActionComplete = null, Action<UIView>? content = null)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void Button(this UIView view, string[]? style = null, string? text = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null)
    // Button — positional-text-first overload. Same rationale as the matching Text overload — avoids CS1744 when models write view.Button("Sign in", onClick: …). First parameter is named buttonText to avoid ambiguity with callers using Button(text: "...") by name.
    static void Button(this UIView view, string buttonText, string[]? style = null, bool? disabled = null, string? href = null, string? type = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, Action<UIView>? content = null)
    // Semantic heading element for titles and section headers.
    static void Heading(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Heading — positional-text-first overload, same rationale as the matching Text overload: view.Heading("Settings", style: [Text.H2]) is the shape models reach for. Parameter is named headingText to avoid ambiguity with callers using text: by name.
    static void Heading(this UIView view, string headingText, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders an icon from an icon library.
    static void Icon(this UIView view, string[]? style = null, string? name = null, IconSize? size = null, string? library = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Icon — positional-name-first overload. Same rationale as the matching Text overload: view.Icon("check", style: [Icon.Sm]) is the shape models reach for. Parameter is named iconName to avoid ambiguity with callers using name: by name.
    static void Icon(this UIView view, string iconName, string[]? style = null, IconSize? size = null, string? library = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Inline anchor link — sugar for a `Button` styled like a hyperlink with an `href`. Mirrors HTML anchor semantics. By default opens in the same tab; pass target: "_blank" to open in a new tab (we automatically add `rel="noopener noreferrer"` for `_blank` if no other `rel` is provided). Generated code naturally reaches for `view.Link(text:, href:)`; this gives it the canonical shape rather than forcing every link into `view.Button(href:, …)`.
    static void Link(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link — positional-text-first overload. Same rationale as the matching Text overload: view.Link("Docs", href: "https://…") is the shape models reach for. Parameter is named linkText to avoid ambiguity with callers using text: by name.
    static void Link(this UIView view, string linkText, string[]? style = null, string? href = null, string? target = null, string? rel = null, Delegate? onClick = null, string? icon = null, Align iconPosition = Start, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Renders markdown content with formatting support.
    static void Markdown(this UIView view, string[]? style = null, string? content = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Markdown — positional-content-first overload: view.Markdown("# Hello"). Parameter is named markdownContent to avoid ambiguity with callers using content: by name.
    static void Markdown(this UIView view, string markdownContent, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Text element for displaying content.
    static void Text(this UIView view, string[]? style = null, string? text = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Text element — positional-text-first overload. Models trained on shadcn / Radix / React conventions reach for view.Text("Hello", style: ["text-xl"]) rather than the view.Text(["text-xl"], "Hello") ordering. Without this overload, the positional string argument fails to bind to the original signature's first parameter (string[]? style), producing CS1744 / CS1503 — the most common compile error in the codegen benchmark. Parameter is named textContent (not text) to avoid ambiguity with existing callers that use Text(text: "...") by name.
    static void Text(this UIView view, string textContent, string[]? style = null, string? href = null, string? target = null, string? rel = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Single toggle button.
    static void Toggle(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null)
    // Item within a toggle group.
    static void ToggleGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle group with multiple selection.
    static void ToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle group with single selection.
    static void ToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    static void AccordionContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Wraps an AccordionTrigger.
    static void AccordionHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for an accordion item.
    static void AccordionItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accordion with multiple items open at a time.
    static void AccordionMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Accordion with single item open at a time.
    static void AccordionSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggles the collapsed state of an accordion item.
    static void AccordionTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Expandable/collapsible container.
    static void Collapsible(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content that is shown or hidden.
    static void CollapsibleContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggles the collapsed state.
    static void CollapsibleTrigger(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Options for downloading a file to the client.
  sealed class DownloadFileActionOptions : ActionOptions, IEquatable<DownloadFileActionOptions>
    ctor()
    // Binary data to download. When set, Url is auto-generated as a data URL.
    byte[]? Data { get; init; }
    // Suggested filename for the downloaded file.
    string? Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Optional — defaults to "application/octet-stream" when DownloadFileActionOptions.Data is set without a MIME type.
    string? MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data using DownloadFileActionOptions.MimeType, falling back to "application/octet-stream" when MimeType is unset so the download still fires.
    string Url { get; init; }
  // Extension methods for drag and drop components.
  static class DragAndDropExtensions
    // Root context for drag and drop operations.
    static void DndContext(this UIView view, string[]? style = null, CollisionDetection collisionDetection = ClosestCenter, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<DragStartArgs, Task>? onDragStart = null, Func<DragMoveArgs, Task>? onDragMove = null, Func<DragOverArgs, Task>? onDragOver = null, Func<DragEndArgs, Task>? onDragEnd = null, Func<Task>? onDragCancel = null, int? activationDistance = null, Action<UIView>? content = null)
    // Overlay shown while dragging.
    static void DragOverlay(this UIView view, string[]? style = null, bool? dropAnimation = true, string? activeDragId = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Element that can be dragged.
    static void Draggable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, bool? hideOnDrag = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drop target area.
    static void Droppable(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, object? data = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Context for sortable list operations.
    static void SortableContext(this UIView view, string[]? style = null, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Drag handle for a SortableItem. When a SortableHandle descendant is present, only pointerdown on the handle starts a drag; the rest of the item remains free for inner clickable elements like buttons. Place inside a SortableItem (or a SortableList itemContent). Outside a SortableItem the handle renders as a plain container.
    static void SortableHandle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Sortable item within a SortableContext.
    static void SortableItem(this UIView view, string[]? style = null, string? id = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // SortableList component that auto-handles reordering.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • listStyle - The container holding all sortable items • itemStyle - Each individual sortable item Example:
    // view.SortableList(
    //     items: _items.Value,
    //     onReorder: async args => _items.Value = args.NewOrder.ToList(),
    //     itemContent: (v, id) => v.Text([Text.Body], id));
    static void SortableList(this UIView view, IReadOnlyList<string>? items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task>? onReorder = null, Func<DragStartArgs, Task>? onDragStart = null, Action<UIView, string>? itemContent = null, string[]? listStyle = null, string[]? itemStyle = null, int? activationDistance = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event args for drag cancel in @dnd-kit.
  sealed class DragCancelArgs : IEquatable<DragCancelArgs>
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed class DragEndArgs : IEquatable<DragEndArgs>
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed class DragMoveArgs : IEquatable<DragMoveArgs>
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed class DragOverArgs : IEquatable<DragOverArgs>
    ctor(string ActiveId, string? OverId)
    string ActiveId { get; init; }
    string? OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed class DragStartArgs : IEquatable<DragStartArgs>
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed class EscapeKeyDownArgs : IEquatable<EscapeKeyDownArgs>
    ctor()
  // Per-client expanded-node state for TreeViewExtensions.TreeView<T> — a reactive set of expanded node ids, so apps don't hand-roll revision counters or per-node booleans.
  // Remarks:
  // Backed by a ClientReactive<T>: each client expands and collapses independently, and reads during UI rendering are dependency-tracked, so the tree re-renders automatically. Access it where a client scope is active (UI render or event handlers).
  sealed class ExpandedSet
    // Create the set, optionally pre-expanding the given node ids for every client.
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
    None
    Image
    Video
    VideoFull
  // Extension methods for the FeedScroller component — a vertically-snapping, full-viewport feed optimized for media-heavy content (TikTok / Reels / Shorts-style).
  // Remarks:
  // Performance model: • Native CSS scroll-snap drives the snap — no JS scroll loop. • Active slide is detected with IntersectionObserver, not scroll events. • Only slides inside [active - preloadBehind, active + preloadAhead] render their content; slides outside the window render as fixed-height spacers that preserve scroll position. • Media declared on FeedScrollerExtensions.FeedSlide is warmed with off-DOM Image/<video> elements as soon as a slide enters the preload window. • Autoplay is gated on the active slide only — neighbour videos are paused.
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    static void FeedScroller(this UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[]? style = null, string[]? slideStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onActiveChange = null, Func<double, Task>? onScrollNearEnd = null, Func<bool, Task>? onMuteChange = null)
    // A single slide inside a FeedScrollerExtensions.FeedScroller. Use when rendering slides manually rather than via the FeedScrollerExtensions.FeedSlide declarative API.
    static void FeedSlide(this UIView view, int index, string[]? style = null, FeedMediaKind mediaKind = None, string? mediaUrl = null, string? mediaPoster = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // A single slide in a FeedScrollerExtensions.FeedScroller.
  sealed class FeedSlide : IEquatable<FeedSlide>
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
    // Native file picker. Emits onFileSelected once per selected file with its metadata (name, mime, size, client-generated selection id). The File bytes stay on the client and are not transferred until a FileUpload with matching seedSelectionIds is mounted.
    static void FilePicker(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FilePickerSelectedArgs, Task>? onFileSelected = null, Func<FilePickerValidationErrorArgs, Task>? onValidationError = null, Action<UIView>? content = null)
  // Metadata for a file chosen in a FilePickerExtensions.FilePicker. The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed class FilePickerSelectedArgs : IEquatable<FilePickerSelectedArgs>
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Reported when client-side validation rejects a picked file (e.g. file too large for maxFileSize). Host UIs should surface FilePickerValidationErrorArgs.Reason to the user — without a handler the rejection is silent and the user just sees "nothing happened" after clicking the picker.
  sealed class FilePickerValidationErrorArgs : IEquatable<FilePickerValidationErrorArgs>
    ctor(string FileName, string MimeType, long Size, string Reason)
    string FileName { get; init; }
    string MimeType { get; init; }
    string Reason { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    static void FileUpload(this UIView view, string[]? style = null, string[]? accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string? capture = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, string[]? seedSelectionIds = null, Action<UIView>? content = null)
    // Wrapper component that adds file upload capability (drag-drop + paste) to any content. Children define the visual appearance.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • zoneStyle - The drop zone container • activeStyle - Style applied when a file is being dragged over the zone Example:
    // view.FileUploadZone(
    //     accept: ["image/*"],
    //     onUploadComplete: async args => { /* handle uploaded file */ },
    //     zoneStyle: ["border-2 border-dashed p-8"],
    //     activeStyle: ["border-primary bg-primary/10"],
    //     content: v => v.Text([Text.Muted], "Drop files here"));
    static void FileUploadZone(this UIView view, string[]? style = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string[]? accept = null, Func<FileUploadPreStartArgs, Task<FileUploadResult>>? onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadResult>>? onUploadStart = null, Func<FileUploadProgressArgs, Task>? onUploadProgress = null, Func<FileUploadCompleteArgs, Task>? onUploadComplete = null, Func<FileUploadErrorArgs, Task>? onUploadError = null, Func<FileUploadChunkArgs, Task>? onChunkReceived = null, Func<bool, Task>? onDragActiveChange = null, Action<UIView>? content = null, string[]? zoneStyle = null, string[]? activeStyle = null, string? activeStyleId = null, string? styleId = null, string? key = null, string[]? seedSelectionIds = null, IReadOnlyDictionary<string, object>? props = null)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    static void FocusHint(this UIView view, FocusHintProps props, string? key = null, Guid? targetViewId = null)
  // Configuration for a focus hint request including priority, ranking, and cooldown behavior.
  sealed class FocusHintProps : IEquatable<FocusHintProps>
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
  sealed class FocusOutsideArgs : IEquatable<FocusOutsideArgs>
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use FormExtensions.TriStateCheckbox.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null)
    // Visual indicator for the checkbox state.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Form container with validation support.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    // Wraps the input control.
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for a form field with label and validation.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Label for a form field.
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Validation message for a form field.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Submit button for the form.
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Accessible label for form controls.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Container for radio buttons.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // Visual indicator for the selected radio.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual radio button.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Range slider control.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null)
    // Single-thumb slider with a scalar value — the common case. Sugar over the list form so callers write Slider(value: 50, onValueChange: async v => …) without the one-element-list dance. Use the list form for multi-thumb ranges.
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    // Filled range portion of the slider.
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Draggable thumb on the slider.
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Track for the slider.
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toggle switch control.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null)
    // The thumb that moves when the switch is toggled.
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null)
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
    Hour24
    Hour12
  // Size of an Icon — the size: form of the Theming.Icon.Xs..Xl tokens, so an icon sizes the same way a Spinner does (size: IconSize.Lg). The style-array form (view.Icon([Icon.Lg], name: "check")) stays valid and, being a caller class, still wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
  // Event returned from an image capture action with the captured image data.
  sealed class ImageCaptureActionEvent : ActionEvent, IEquatable<ImageCaptureActionEvent>
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Fallback content shown when image fails to load.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Image element for the avatar.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // Image element.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // Image element with binary data payload.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // Individual input slot for OTP.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Password input with visibility toggle.
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    // Icon that changes based on visibility state.
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // The password input element.
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Button to toggle password visibility.
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Multi-line text input area.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null)
    // Single-line text input field. Passing multiline: true or rows: turns it into a multi-line field by delegating to TextArea.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed class InteractOutsideArgs : IEquatable<InteractOutsideArgs>
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
  sealed class KeyboardEventArgs : IEquatable<KeyboardEventArgs>
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
    static void KeyboardListener(this UIView view, Func<KeyboardEventArgs, Task>? onKeyDown = null, Func<KeyboardEventArgs, Task>? onKeyUp = null, IReadOnlyList<string>? keys = null, bool? global = true, bool? preventDefault = null, bool? stopPropagation = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    static void AspectRatio(this UIView view, string[]? style = null, double ratio = 1, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Provides text direction context (ltr/rtl) to descendants.
    static void DirectionProvider(this UIView view, string[]? style = null, Dir dir = Ltr, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Infinite scroll view that fires callbacks when user scrolls near the end.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • viewportStyle - The scrollable viewport (use ScrollArea.Viewport) • scrollbarStyle - The scrollbar track (use ScrollArea.Scrollbar) • thumbStyle - The scrollbar thumb (use ScrollArea.Thumb) • rootStyle - The outermost container (rarely needed) Example:
    // view.InfiniteScrollView(
    //     hasMore: _hasMoreData.Value,
    //     loading: _isLoading.Value,
    //     onNearEnd: async args => await LoadMoreItems(),
    //     content: v => { /* list items */ });
    static void InfiniteScrollView(this UIView view, string[]? style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView>? loadingIndicator = null, Func<ScrollNearEndArgs, Task>? onNearEnd = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Progress component that auto-renders the indicator with transform. SemanticTone.Success, SemanticTone.Warning, and SemanticTone.Error tones map to the matching Theming.Progress.Variant tokens; other tones use the default (brand) fill.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • rootStyle - The progress track/container (use Progress.Root) • indicatorStyle - The filled indicator bar (use Progress.Indicator) Example:
    // view.Progress(value: 50, max: 100, rootStyle: [Progress.Root]);
    // view.Progress(indeterminate: true, rootStyle: [Progress.Root]);
    static void Progress(this UIView view, string[]? style = null, double? value = null, double? max = null, SemanticTone tone = Neutral, bool indeterminate = false, Func<double?, string>? getValueLabel = null, string[]? rootStyle = null, string[]? indicatorStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via onResized.
    static void ResizableSplit(this UIView view, Orientation orientation = Horizontal, double initialSize = 200, double minSize = 100, double maxSize = 500, bool reversed = false, Func<double, Task>? onResized = null, Action<UIView>? first = null, Action<UIView>? second = null, string[]? style = null, string[]? handleStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea component that auto-renders viewport and scrollbars.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • viewportStyle - The scrollable viewport (use ScrollArea.Viewport) • scrollbarStyle - The scrollbar track (use ScrollArea.Scrollbar) • thumbStyle - The scrollbar thumb (use ScrollArea.Thumb) • cornerStyle - The corner element when both scrollbars are visible • rootStyle - The outermost container (rarely needed) Example:
    // view.ScrollArea(
    //     scrollbars: ScrollAreaScrollbars.Vertical,
    //     content: v => { /* scrollable content */ });
    static void ScrollArea(this UIView view, string[]? style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, object? autoScrollKey = null, Action<UIView>? content = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string[]? cornerStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // ScrollArea — positional (style, children) overload (see ContainerExtensions.Box).
    static void ScrollArea(this UIView view, string[]? style, Action<UIView> children)
    // Visual separator between content.
    static void Separator(this UIView view, string[]? style = null, Orientation orientation = Horizontal, bool decorative = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed class LocationActionEvent : ActionEvent, IEquatable<LocationActionEvent>
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  // Specifies the behavior of a CaptureButton when pressed.
  enum MediaCaptureButtonMode
    Hold
    Toggle
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind. MediaCaptureEvent.ClientContext identifies the user who initiated the capture and is populated for all capture kinds (audio, camera, screen). Prefer reading MediaCaptureEvent.ClientSessionId / MediaCaptureEvent.UserId rather than tracking streamId-to-client mappings yourself.
  sealed class MediaCaptureEvent : IEquatable<MediaCaptureEvent>
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
    Audio
    Camera
    Screen
  // Extension methods for media playback components.
  static class MediaExtensions
    // Audio player for URL-based audio content.
    static void AudioUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string? preload = null, string? styleId = null, string? key = null)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void CaptureButton(this UIView view, string[]? style = null, MediaCaptureKind kind = Audio, string? text = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions? audioOptions = null, ClientVideoCaptureOptions? videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Push-to-talk microphone button: a CaptureButton(kind: Audio, captureMode: Hold) that integrates with Audio.SpeechRecognizedAsync. After enabling speech recognition once (Audio.UseSpeechRecognition(...)), subscribe to Audio.SpeechRecognizedAsync to receive transcriptions when the user releases the button. The user's client context is carried on the event args — no streamId-to-client plumbing needed in the app.
    static void PushToTalkButton(this UIView view, string[]? style = null, string? text = "⏺", int holdReleaseDelayMs = 500, ClientAudioCaptureOptions? audioOptions = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<MediaCaptureEvent, Task>? onCaptureStart = null, Func<MediaCaptureEvent, Task>? onCaptureStop = null, Action<UIView>? content = null)
    // Canvas element for rendering a live video stream.
    static void VideoStreamCanvas(this UIView view, string[]? style = null, string? streamId = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
    // Video player for URL-based video content.
    static void VideoUrlPlayer(this UIView view, string[]? style = null, string? url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string? poster = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    static void Menubar(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Checkbox item in menu.
    static void MenubarCheckboxItem(this UIView view, string[]? style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onCheckedChange = null, Action<UIView>? content = null)
    // Dropdown content for the menu.
    static void MenubarContent(this UIView view, string[]? style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Clickable menu item.
    static void MenubarItem(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // Visual indicator for checkbox/radio state.
    static void MenubarItemIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual menu in the menubar.
    static void MenubarMenu(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Radio group in menu.
    static void MenubarRadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Radio item in menu.
    static void MenubarRadioItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator between menu items.
    static void MenubarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Submenu container.
    static void MenubarSub(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Content for submenu.
    static void MenubarSubContent(this UIView view, string[]? style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger for submenu.
    static void MenubarSubTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button that opens a menu.
    static void MenubarTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Navigation menu root.
    static void NavigationMenu(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Content shown when navigation item is active.
    static void NavigationMenuContent(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Visual indicator for active navigation item.
    static void NavigationMenuIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Individual navigation menu item.
    static void NavigationMenuItem(this UIView view, string[]? style = null, string? value = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Link within navigation menu.
    static void NavigationMenuLink(this UIView view, string[]? style = null, bool? active = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onSelect = null, Action<UIView>? content = null)
    // List of navigation menu items.
    static void NavigationMenuList(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Trigger that opens navigation content.
    static void NavigationMenuTrigger(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Viewport for navigation menu content.
    static void NavigationMenuViewport(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Toolbar container.
    static void Toolbar(this UIView view, string[]? style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Button in the toolbar.
    static void ToolbarButton(this UIView view, string[]? style = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null, Action<UIView>? content = null)
    // Link in the toolbar.
    static void ToolbarLink(this UIView view, string[]? style = null, string? href = null, string? target = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Separator in the toolbar.
    static void ToolbarSeparator(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // Multi-select toggle group in toolbar.
    static void ToolbarToggleGroupMultiple(this UIView view, string[]? style = null, IReadOnlyList<string>? value = null, IReadOnlyList<string>? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<string>, Task>? onValueChange = null, Action<UIView>? content = null)
    // Single-select toggle group in toolbar.
    static void ToolbarToggleGroupSingle(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? rovingFocus = true, bool loop = true, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null)
    // Toggle item in toolbar toggle group.
    static void ToolbarToggleItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard, Toast). Each handles Portal/Overlay management automatically.
  static class OverlayExtensions
    // Alert dialog that requires explicit user acknowledgment. Cannot be dismissed by clicking outside.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • overlayStyle - The background overlay (use AlertDialog.Overlay) • contentStyle - The dialog content container (use AlertDialog.Content) • titleStyle - The title text (use AlertDialog.Title) • descriptionStyle - The description text (use AlertDialog.Description) • footerStyle - The button container (use AlertDialog.Footer) • cancelStyle - The cancel button (use AlertDialog.Cancel) • actionStyle - The action button (use AlertDialog.Action) • rootStyle - The outermost container (rarely needed) Example:
    // view.AlertDialog(
    //     title: "Are you sure?",
    //     description: "This action cannot be undone.",
    //     actionLabel: "Delete",
    //     onAction: async () => { /* handle delete */ },
    //     overlayStyle: [AlertDialog.Overlay],
    //     contentStyle: [AlertDialog.Content],
    //     trigger: v => v.Button([Button.ErrorMd], text: "Delete"));
    static void AlertDialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, string? title = null, string? description = null, string? cancelLabel = null, string? actionLabel = null, Func<Task>? onAction = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string[]? cancelStyle = null, string[]? actionStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Action<UIView>? content = null)
    // Modal dialog window.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • overlayStyle - The background overlay (use Dialog.Overlay) • contentStyle - The dialog content container (use Dialog.Content) • rootStyle - The outermost container (rarely needed) Example:
    // view.Dialog(
    //     overlayStyle: [Dialog.Overlay],
    //     contentStyle: [Dialog.Content],
    //     trigger: v => v.Button([Button.PrimaryMd], text: "Open"),
    //     contentSlot: v => v.Text([Text.Body], "Dialog content"));
    static void Dialog(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? overlayStyle = null, string? overlayStyleId = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? headerStyle = null)
    // Rich content card that appears on hover with configurable delays.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • contentStyle - The hover card content (use HoverCard.Content) • rootStyle - The hover card container (rarely needed) Example:
    // view.HoverCard(
    //     contentStyle: [HoverCard.Content],
    //     trigger: v => v.Text([Text.Link], "@username"),
    //     contentSlot: v => { ... });
    static void HoverCard(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Floating content panel that appears next to a trigger element.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • contentStyle - The popover content container (use Popover.Content) • rootStyle - The outermost container (rarely needed) Example:
    // view.Popover(
    //     contentStyle: [Popover.Content],
    //     trigger: v => v.Button([Button.PrimaryMd], text: "Open"),
    //     contentSlot: v => v.Text([Text.Body], "Popover content"));
    static void Popover(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // Toast notification with built-in provider and viewport.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • toastStyle - The toast container (use Toast.Root) • viewportStyle - The viewport where toasts appear (use Toast.Viewport) • titleStyle - The title text (use Toast.Title) • descriptionStyle - The description text (use Toast.Description) • closeStyle - The close button (use Toast.Close) Example:
    // view.Toast(
    //     title: "Saved!",
    //     description: "Your changes have been saved.",
    //     toastStyle: [Toast.Root],
    //     titleStyle: [Toast.Title],
    //     descriptionStyle: [Toast.Description]);
    static void Toast(this UIView view, string[]? style = null, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string? title = null, string? description = null, bool? showClose = null, string? closeLabel = null, Action<UIView>? content = null, string[]? toastStyle = null, string[]? viewportStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null, Func<Task>? onEscapeKeyDown = null, Func<Task>? onPause = null, Func<Task>? onResume = null, Func<ToastSwipeArgs, Task>? onSwipeStart = null, Func<ToastSwipeArgs, Task>? onSwipeMove = null, Func<ToastSwipeArgs, Task>? onSwipeEnd = null, Func<ToastSwipeArgs, Task>? onSwipeCancel = null)
    // Brief informational message that appears on hover. Includes built-in provider.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • contentStyle - The tooltip content (use Tooltip.Content) • rootStyle - The tooltip container (rarely needed) Example:
    // view.Tooltip(
    //     contentStyle: [Tooltip.Content],
    //     trigger: v => v.Button([Button.GhostMd], text: "Hover me"),
    //     contentSlot: v => v.Text([Text.Caption], "Helpful hint"));
    static void Tooltip(this UIView view, string[]? style = null, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView>? trigger = null, Action<UIView>? contentSlot = null, Action<UIView>? content = null, string[]? contentStyle = null, string? contentStyleId = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
  // Composite overlay-menu components built on the Popover/Dialog primitives and the Menu/Combobox/ Command theme tokens — the shadcn Combobox, DropdownMenu, Command-palette, and Kbd, expressed as C# composites (no bespoke node type). Filtering is server-side over the app's reactive search state, matching Parallax's reactive model; client-side typeahead/roving-focus is a later renderer concern, not required for the components to work.
  static class OverlayMenuExtensions
    // A searchable Select (the shadcn Combobox): a Popover whose trigger shows the current selection and whose panel is a search field over the filtered options. Filtering is server-side — bind searchValue to a reactive and echo edits via onSearchChange, and the list narrows by case-insensitive label match. Without a bound search value it renders as a Popover-select (no filtering).
    static void Combobox(this UIView view, IReadOnlyList<SelectOption> options, string? value = null, Func<string, Task>? onValueChange = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, bool? open = null, Func<bool, Task>? onOpenChange = null, string? placeholder = "Select…", string? searchPlaceholder = "Search…", string? emptyText = "No results.", string[]? style = null, string[]? triggerStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // A command palette (the shadcn Command in a dialog): a centred search field over a grouped, filtered action list. Filtering is server-side over searchValue — each group's options narrow by case-insensitive label match, and empty groups drop out. onSelect fires with the chosen option's value.
    static void CommandPalette(this UIView view, IReadOnlyList<SelectOptionGroup> groups, bool? open = null, Func<bool, Task>? onOpenChange = null, Func<string, Task>? onSelect = null, string? searchValue = null, Func<string, Task>? onSearchChange = null, string? placeholder = "Type a command or search…", string? emptyText = "No results.", string[]? panelStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // A dropdown menu — a Popover preset with the menu content surface. Fill content with view.Button([Menu.Item]) / [Menu.ItemDestructive] rows and Menu.Label / Menu.Separator; the component supplies the trigger wiring and the menu-shaped popover panel.
    static void DropdownMenu(this UIView view, Action<UIView> trigger, Action<UIView> content, bool? open = null, Side side = Bottom, Align align = Start, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onOpenChange = null)
    // A keyboard-key chip — the shadcn Kbd. Pass a single text for one key, or keys for a combo (each key its own chip, spaced).
    static void Kbd(this UIView view, string? text = null, IReadOnlyList<string>? keys = null, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // One page of items plus the controls needed to render prev/next buttons. Returned by PaginationExtensions.Paginate<T>.
  sealed class Page<T> : IEquatable<Page<T>>
    ctor(IReadOnlyList<T> Items, int Index, int TotalPages, int PageSize, bool CanPrev, bool CanNext, Func<Task> Prev, Func<Task> Next, Func<int, Task> JumpTo, Func<Task> First, Func<Task> Last, IReadOnlyList<T> Source)
    // True if there is a next page.
    bool CanNext { get; init; }
    // True if there is a previous page.
    bool CanPrev { get; init; }
    // Action that jumps to page 0.
    Func<Task> First { get; init; }
    // Zero-based current page index.
    int Index { get; init; }
    // The slice of Page<T>.Source for the current page.
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
    // Total number of pages (always >= 1, even when Page<T>.Source is empty).
    int TotalPages { get; init; }
  // Bounded-cursor primitive on top of ClientReactive<T>. Slices an in-memory list, returns the slice + bound actions (Prev/Next/JumpTo/First/Last) the caller binds to whatever UI fits. Holds zero rendering opinion — no tab bars, no default control rows, no opinionated layout. Most Ikon apps don't need pagination at all (live feeds, autoscroll, virtualization handle the common cases via ReactiveList<T> + ScrollArea(autoScroll: true)). Use this when you have a static list large enough to warrant explicit page navigation. For DB-backed pagination (load only the current page from a backend), drive ClientReactive<T> directly and observe its value in your data-loading code — same per-client semantics, no special helper needed.
  static class PaginationExtensions
    // Slice items by pageSize using page as per-client current-page state. Each connected client sees its own page; setting page from one client doesn't shift another client's view.
    static Page<T> Paginate<T>(this UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed class PickContactsActionOptions : ActionOptions, IEquatable<PickContactsActionOptions>
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed class PointerDownOutsideArgs : IEquatable<PointerDownOutsideArgs>
    ctor(string? TargetId)
    string? TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    static void QR(this UIView view, string[]? style = null, string? value = null, int size = 256, string? key = null)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Inline rich-text editor with a configurable toolbar. Values are HTML strings.
    static void RichTextEditor(this UIView view, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool>? tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[]? style = null, string[]? toolbarStyle = null, string[]? toolbarButtonStyle = null, string[]? contentStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null)
  // Formatting action available in the RichTextEditorExtensions.RichTextEditor toolbar.
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
    // Renders a flex column with an optional header, a scrollable body, and an optional footer. The header and footer stay pinned; the body scrolls.
    // Remarks:
    // This is the canonical shape for dialogs, side panels, and chat-style layouts where you need a fixed chrome around an overflowing region. Using this helper avoids the common pitfall of a flex-1 ScrollArea that won't shrink inside a flex parent (a CSS flexbox quirk: min-height: auto by default). The outer container's height is the caller's responsibility — set it via the style parameter (for example "h-[82vh]") or let a flex-1 parent provide bounds. Example — dialog with header + scrolling body + composer:
    // view.ScrollColumn(
    //     style: ["h-[82vh] w-full sm:max-w-[560px] rounded-2xl bg-white"],
    //     header: h => h.Row(["px-5 py-4 border-b"], content: ...),
    //     footer: f => f.Row(["p-3 border-t"], content: ...),
    //     content: body => body.Column(["gap-3"], content: ...));
    static void ScrollColumn(this UIView view, string[]? style = null, Action<UIView>? header = null, Action<UIView>? footer = null, Action<UIView>? content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, object? autoScrollKey = null, string[]? bodyStyle = null, string[]? viewportStyle = null, string[]? scrollbarStyle = null, string[]? thumbStyle = null, string? styleId = null, string? key = null)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed class ScrollNearEndArgs : IEquatable<ScrollNearEndArgs>
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, ScrollDirection Direction)
    double ClientHeight { get; init; }
    ScrollDirection Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // Select dropdown component that auto-renders the full structure with trigger button, dropdown content, and items. Use either options (flat list) or groups (grouped items) - not both.
    // Remarks:
    // An Input.* token passed as the Select's own style is ignored (with a dev warning): it would style the outer wrapper, not the field-shaped element. The trigger already carries the field theme — customize it through triggerStyle. For trigger sizing, use Select.Size tokens: triggerStyle: [Select.Size.Sm] for small, triggerStyle: [Select.Size.Lg] for large. The default size is medium.
    static void Select(this UIView view, string[]? style = null, IReadOnlyList<SelectOption>? options = null, IReadOnlyList<SelectOptionGroup>? groups = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string? name = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? itemStyle = null, string[]? itemIndicatorStyle = null, string? indicatorIconName = "check", string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null, Reactive<string>? bind = null)
  // Represents a selectable option in a Select component.
  sealed class SelectOption : IEquatable<SelectOption>
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed class SelectOptionGroup : IEquatable<SelectOptionGroup>
    ctor(string? Label, IReadOnlyList<SelectOption> Options)
    string? Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
  // Semantic tone shared by the app-chrome components (Badge, Alert, Toasts, StatCard icon box). Tones map to the theme's semantic color tokens, so they are correct in both light and dark mode.
  enum SemanticTone
    Neutral
    Brand
    Success
    Warning
    Error
    Info
  // A typed uniform value to pass to a WebGL shader. Use the static factory methods to create instances.
  struct ShaderUniform
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
    // Shadertoy-compatible WebGL fragment shader canvas.
    // Remarks:
    // Renders GLSL fragment shaders with Shadertoy-compatible uniforms. The shader code must define a mainImage function with signature: void mainImage(out vec4 color, in vec2 fragCoord) Built-in uniforms (automatically provided): • iResolution (vec3) - canvas width, height, and 1.0 • iTime (float) - elapsed time in seconds • iTimeDelta (float) - time since last frame • iFrame (int) - current frame number • iMouse (vec4) - mouse x, y, click x, click y (requires enableMouse=true) • iDate (vec4) - year, month, day, seconds of day Texture channels: Pass image URLs (data URIs or http(s)) via channels to bind them to the Shadertoy channel uniforms, matching Shadertoy's default sampler behavior so shaders copied from shadertoy.com that sample 2D textures render the same way: • iChannel0..iChannel3 (sampler2D) - channel textures, in array order • iChannelResolution[4] (vec3) - per-channel pixel size (0 until loaded) • iChannelTime[4] (float) - always 0 for static images Textures use Shadertoy's defaults: vertical flip on (upright with uv = fragCoord/iResolution), repeat wrap, and mipmap filtering. Sample with texture(iChannel0, uv). Limitations: 2D image channels only - no cubemap (samplerCube), buffer, audio, or video channels; single output only.
    static void ShadertoyCanvas(this UIView view, string[]? style = null, string? shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform>? uniforms = null, IReadOnlyList<string>? channels = null, bool? enableMouse = null, int? width = null, int? height = null, string? styleId = null, string? key = null)
  // Options for the Web Share API action.
  sealed class ShareActionOptions : ActionOptions, IEquatable<ShareActionOptions>
    ctor()
    // Text body for the shared content.
    string? Text { get; init; }
    // Title for the shared content.
    string? Title { get; init; }
    // URL to share.
    string? Url { get; init; }
  // Slide-over panel composites (Sheet, Drawer) built on the Dialog primitive. The dialog's portal + content styling is repositioned per side via the Theming.Sheet / Theming.Drawer token recipes, including Crosswind slide-in/out motion classes driven by the panel's data-state attribute.
  static class SheetExtensions
    // Bottom drawer on top of the Dialog primitive — mobile-style rounded panel with a drag handle, per the Theming.Drawer token recipe. Same open/close model as SheetExtensions.Sheet: in controlled mode pass onOpenChange to actually close.
    static void Drawer(this UIView view, bool? open = null, Func<bool, Task>? onOpenChange = null, string? title = null, string? description = null, Action<UIView>? trigger = null, Action<UIView>? content = null, Action<UIView>? footer = null, bool? defaultOpen = null, bool? modal = null, bool showHandle = true, string[]? style = null, string[]? overlayStyle = null, string[]? handleStyle = null, string[]? headerStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? footerStyle = null, string? key = null)
    // Side-anchored slide-over panel on top of the Dialog primitive. With zero style args the panel uses Theming.Sheet.Base plus the side token (position, border, slide animation); caller styles merge on top, and the literal "unstyled" class opts out. In controlled mode (open set) pass onOpenChange and flip your state to false there, or the built-in close button and outside clicks cannot dismiss the sheet.
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
    // Renders content live, but omits it entirely from the boot snapshot — use to keep a region out of the public snapshot without leaving even a skeleton (e.g. interactive controls that are dead before the live connection).
    static void SnapshotHide(this UIView view, Action<UIView> content)
    // Renders content only in the boot snapshot, never live — use for snapshot-specific filler (e.g. a curated first-paint placeholder) that should disappear once the live UI takes over. The filler is rendered as authored (not auto-skeletonized), since it is the developer's own snapshot stand-in.
    static void SnapshotOnly(this UIView view, Action<UIView> content)
    // Opts content out of automatic skeletonization: it renders as real content in the boot snapshot instead of being replaced with skeletons. Use only for content that is safe to bake into the public snapshot (logos, static chrome, marketing copy). The opt-out applies to the whole subtree — nested containers and leaves all render their real content. IsSnapshot stays true inside the region, so this means "show real content here", not "render as if live".
    static void SnapshotReveal(this UIView view, Action<UIView> content)
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed class SortableReorderArgs : IEquatable<SortableReorderArgs>
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
    Flat
    Up
    Down
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  class TabItem : IEquatable<TabItem>
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
    // Table container (CSS display: table). Compose with TableExtensions.TableHeader, TableExtensions.TableBody, TableExtensions.TableRow, TableExtensions.TableHead, and TableExtensions.TableCell. Caller styles merge on top of the base token; include the literal "unstyled" class to opt out.
    static void Table(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // Table — positional (style, children) overload.
    static void Table(this UIView view, string[]? style, Action<UIView> children)
    // Body row group (CSS display: table-row-group).
    static void TableBody(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Data cell (CSS display: table-cell).
    static void TableCell(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableCell — positional-text-first overload: r.TableCell(user.Name).
    static void TableCell(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header cell (CSS display: table-cell) with muted uppercase column-label styling.
    static void TableHead(this UIView view, string[]? style = null, string? text = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // TableHead — positional-text-first overload: r.TableHead("Name").
    static void TableHead(this UIView view, string text, string[]? style = null, string? key = null, Action<UIView>? content = null)
    // Header row group (CSS display: table-header-group). Put one TableExtensions.TableRow of TableExtensions.TableHead cells inside.
    static void TableHeader(this UIView view, string[]? style = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
    // Table row (CSS display: table-row) with a bottom border. Rows with onClick also get hover highlight + pointer cursor.
    static void TableRow(this UIView view, string[]? style = null, bool striped = false, Delegate? onClick = null, string? styleId = null, string? key = null, Action<UIView>? content = null)
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Container for Tabs components. Use the 'tabs' parameter to define tab content.
    // Remarks:
    // Styling: This component has multiple style parameters for different parts: • listStyle - The tab list container (use Tabs.List) • triggerStyle - Each tab trigger button (use Tabs.Trigger) • contentStyle - Each tab content panel (use Tabs.Content) • rootStyle - The outermost container (rarely needed) Example:
    // view.Tabs(
    //     listStyle: [Tabs.List],
    //     triggerStyle: [Tabs.Trigger],
    //     contentStyle: [Tabs.Content],
    //     tabs: [...]);
    static void Tabs(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem>? tabs = null, string[]? listContainerStyle = null, string[]? listStyle = null, string[]? triggerStyle = null, string[]? disabledTriggerStyle = null, string[]? contentContainerStyle = null, string[]? contentStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null)
  // Smallest time unit shown by a TimePickerExtensions.TimePicker.
  enum TimeGranularity
    Hour
    Minute
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Picker for a time of day. Values are ISO-8601 HH:mm or HH:mm:ss strings.
    static void TimePicker(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string? placeholder = null, string[]? triggerStyle = null, string[]? contentStyle = null, string[]? columnStyle = null, string[]? itemStyle = null, string[]? itemSelectedStyle = null, string[]? rootStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<bool, Task>? onOpenChange = null, string? label = null)
  // One notification held by a Toasts queue.
  sealed class ToastItem : IEquatable<ToastItem>
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
  sealed class ToastSwipeArgs : IEquatable<ToastSwipeArgs>
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
  // Imperative per-client toast queue so app code never owns notification state.
  // Remarks:
  // Wiring: construct one instance as an app field, mount ToastsExtensions.ToastHost once in the root UI, then fire notifications from any event handler:
  // private readonly Toasts _toasts = new();
  //
  // // in UI.Root(...):
  // view.ToastHost(_toasts);
  //
  // // in any handler:
  // _toasts.Success("Saved");
  // _toasts.Error("Upload failed", "The file exceeds 10 MB.");
  // State lives in a ClientReactive<T>, so each client sees only its own toasts and the host re-renders automatically. Methods must therefore be called where a client scope is active (UI render or event handlers) — the normal places notifications originate. Auto-dismiss is client-driven: the toast primitive counts down ToastItem.DurationMs and reports the close, which removes the item from the queue.
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
    long Show(string title, string? description = null, SemanticTone tone = Neutral, int durationMs = 5000)
    // Enqueue a success toast.
    long Success(string title, string? description = null, int durationMs = 5000)
    // Enqueue a warning toast.
    long Warning(string title, string? description = null, int durationMs = 5000)
    // Default auto-dismiss duration in milliseconds.
    const int DefaultDurationMs
  // Host composite that renders a Toasts queue with the toast primitives.
  static class ToastsExtensions
    // Render the toast viewport for a Toasts queue. Mount exactly once in the root UI; every queued toast renders as a themed toast (tone icon, title, description, close button) that the client auto-dismisses after its duration. Both auto-dismiss and the close button report back and remove the item from the queue.
    static void ToastHost(this UIView view, Toasts toasts, string[]? viewportStyle = null, string[]? toastStyle = null, string[]? titleStyle = null, string[]? descriptionStyle = null, string[]? closeStyle = null, bool showClose = true)
  // Recursive tree composite over the Collapsible primitive, styled with the NavPanel/NavItem token recipes.
  static class TreeViewExtensions
    // Hierarchical tree view. Branch nodes render as Collapsibles whose trigger row toggles expansion (tracked in expanded) and reports selection; leaf nodes are plain clickable rows. The row matching selectedId renders with the active item style.
    static void TreeView<T>(this UIView view, IReadOnlyList<T> roots, Func<T, string> id, Func<T, string> label, Func<T, IReadOnlyList<T>?> children, ExpandedSet expanded, string[]? style = null, Func<T, Task>? onSelect = null, string? selectedId = null, Func<T, string?>? icon = null, string[]? itemStyle = null, string[]? selectedItemStyle = null, string[]? labelStyle = null, string[]? childrenStyle = null, string? styleId = null, string? key = null)
  // Extension methods for the DOM-virtualized scroll containers VirtualListExtensions.VirtualList and VirtualListExtensions.VirtualGrid. Items outside the visible window plus an overscan buffer have their content children skipped at the React layer (the wrapper still occupies space via fixed dimensions), so DOM size scales with viewport, not itemCount.
  // Remarks:
  // Performance model: • Server emits one wrapper node per item up to itemCount; per-item content builders run server-side eagerly. Inexpensive content trees are fine even at large counts. • Client React component watches scroll on the viewport, computes the visible row range from itemHeight/rowHeight + scrollTop, and only mounts children inside [start - overscan, end + overscan]. Out-of-window wrappers render as empty fixed-height placeholders. • onNearEnd fires when the visible window enters the last nearEndThreshold rows. Append more items to grow the list — no special prepend/append API needed.
  static class VirtualListExtensions
    // DOM-virtualized scrollable grid. Items are laid out in a fixed number of columns and rows outside the visible window are not mounted in the DOM.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    // DOM-virtualized vertical list with fixed item height. Renders only items inside the visible window plus an overscan buffer.
    static void VirtualList(this UIView view, int itemCount, double itemHeight, Action<UIView, int> onRenderItem, int overscan = 4, Func<int, Task>? onNearEnd = null, int nearEndThreshold = 5, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Theming
  static class Accessibility
    static string RequiredLabel(string baseLabel)
    const string NotScreenReaderOnly
    const string ScreenReaderOnly
    const string SkipLink
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
  // One vocabulary entry: an accepted theme key and the canonical variable keys it commits. Targets are always canonical (never other aliases), so expansion is one step.
  sealed class ThemeVocabulary.Alias : IEquatable<ThemeVocabulary.Alias>
    ctor(string Name, IReadOnlyList<string> Targets, ThemeVocabulary.ValueKind Kind)
    ThemeVocabulary.ValueKind Kind { get; init; }
    string Name { get; init; }
    IReadOnlyList<string> Targets { get; init; }
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
  static class AspectRatio
    const string Base
    const string Default
    const string PlaceholderContent
  static class Avatar
    const string Base
    const string Default
    const string Fallback
    const string Image
    const string Root
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
  static class Tokens.Blur
    const string Lg
    const string Md
    const string Sm
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
  static class OnSurface.Card
    const string Caption
    const string Muted
    const string Subtle
    const string Text
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
  static class Layout.Column
    const string Center
    const string Default
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
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
  static class OnSurface.Default
    const string Caption
    const string Muted
    const string Subtle
    const string Text
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
  static class Drawer
    const string Content
    const string Default
    const string Description
    const string Footer
    const string Handle
    const string Header
    const string Overlay
    const string Title
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
  static class DragDrop.Droppable
    const string Base
    const string Default
    const string Disabled
    const string Info
    const string Success
  static class Tokens.Duration
    const string Fast
    const string Instant
    const string Normal
    const string Slow
    const string Slower
  static class Transition.Ease
    const string In
    const string InOut
    const string Linear
    const string Out
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
  static class Accessibility.Focus
    const string HighContrast
    const string None
    const string Sentinel
    const string Within
  static class FormField
    const string ErrorText
    const string HelpText
    const string Label
    const string LabelRequired
    const string ParamRow
    const string Root
    const string SuccessText
    const string WarningText
  static class Layout.Grid
    const string Cols2
    const string Cols3
    const string Cols4
  static class Select.Group
    const string Label
    const string Root
  static class ImageCard.Hover
    const string Dim
    const string Zoom
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
  static class FileUpload.Icon
    const string Base
    const string Brand
    const string Disabled
    const string Error
    const string Info
    const string Success
  static class Toggle.Size.Icon
    const string Lg
    const string Md
    const string Sm
  // Per-app theme configuration. Composes the platform's Ikon CSS baseline with per-token CSS-variable overrides addressed by name. One uniform syntax: an indexer keyed by a vocabulary alias (ThemeVocabulary), a CSS variable name (without the leading --), or a Tailwind utility token. The renderer dispatches by key shape: • Vocabulary alias (primary, card, radius, density) → its canonical variable cluster • Tailwind palette step (amber-400) → --color-amber-400 (Ikon scales like neutral-900 also set the bare var) • rounded-{rung} → --radius-{rung} • shadow-{rung} → --shadow-{rung} • font-{role} → --font-{role} • spacing → the --spacing density unit • Anything else → --{key} (free CSS variable) Values are Crosswind / Tailwind class names, which are resolved to CSS, or raw CSS values (hex, rem, family stacks, gradients), which pass through unchanged. Example — the structural core is a small committed set; expressive decoration (gradients, textures) stays concrete at use points:
  // private UI UI { get; } = new(app, new IkonTheme
  // {
  //     // Base overrides restyle the LIGHT theme; DarkMode restyles the dark one.
  //     ["primary"]    = "amber-400",   // whole brand cluster: fills, CTA, focus ring, brand icons/text
  //     ["background"] = "zinc-50",
  //     ["card"]       = "white",
  //     ["foreground"] = "zinc-950",
  //     ["muted-foreground"] = "zinc-500",
  //     ["border"]     = "zinc-200",
  //
  //     ["font-heading"] = "Crimson Pro",
  //     ["font-body"]    = "Inter",
  //     ["radius"]       = "rounded-lg",
  //     ["density"]      = "airy",
  //     ["motion-duration-base"] = "200ms",
  //     ["ease-default"]         = "ease-out",
  //
  //     // Per-token palette / radius / shadow overrides and free decorative vars.
  //     ["amber-400"]  = "#F5A524",
  //     ["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",
  //     ["hero-glow"]  = "radial-gradient(circle, #F5A52488, transparent 70%)",
  //
  //     DarkMode = new IkonTheme
  //     {
  //         ["background"] = "zinc-950",
  //         ["card"]       = "zinc-900",
  //         ["foreground"] = "amber-50",
  //         ["muted-foreground"] = "zinc-400",
  //         ["border"]     = "zinc-800",
  //     },
  // });
  // Aliases expand to exactly their documented cluster — beyond that there is no magic fan-out and no auto-derived contrast text. A later explicit entry overrides an alias-expanded one (["primary"] then ["bg-brand-button"] re-pins just the CTA).
  sealed class IkonTheme : ITheme
    ctor()
    // Paired dark-mode theme. Pass another IkonTheme; its overrides are emitted under [data-theme="dark"], .dark, and prefers-color-scheme: dark. Valid only in ThemeMode.Adaptive mode: combining it with ThemeMode.Fixed — which commits to a single scheme — is a contradiction and throws InvalidOperationException at render time.
    IkonTheme? DarkMode { get; init; }
    // Per-token override addressed by CSS variable name (without the leading --) or by Tailwind utility token. Set during object initialization.
    string this[string token] { get; set; }
    // How the app relates to light/dark switching. ThemeMode.Adaptive (the default) keeps today's behavior: overrides restyle the light theme, IkonTheme.DarkMode restyles the dark one, and the client's theme preference picks between them. ThemeMode.Fixed commits to ONE scheme: every override is also emitted under the dark selectors, so a client-side theme flip cannot pull the platform's dark palette in under the app's committed colors. For atmospheric, game, or brand-locked looks that should never light/dark switch.
    ThemeMode Mode { get; init; }
  static class ImageCard
    const string Caption
    const string Image
    const string Root
    const string Title
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
  static class Interaction
    const string HoverCard
    const string HoverGlow
    const string HoverLift
  static class DragDrop.Item
    const string Base
    const string Dashed
    const string Default
    const string Disabled
    const string Dragging
  // Keyboard-key display (the shadcn Kbd): a small inset chip for a shortcut key or combo. Complete default-marked composite for view.Kbd; the Kbd.Group wrapper spaces several keys in a combo.
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
    const string Item
    // The destructive row (Log out, Delete) — error text with an error-tinted hover, same geometry as Menu.Item.
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
  static class Accessibility.Motion
    const string Reduce
    const string ReduceFade
    const string Respectful
    const string Safe
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
  static class OnSurface
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
  static class Separator.Orientation
    const string Horizontal
    const string Vertical
  static class OtpField
    const string Default
    const string Input
    const string Root
  static class ImageCard.Overlay
    const string Center
    const string Dim
    const string Reveal
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
  static class Input.Password
    const string Input
    const string Toggle
    const string Wrapper
  static class Popover
    const string Content
    const string Default
  static class OnSurface.Popover
    const string Caption
    const string Muted
    const string Subtle
    const string Text
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
  static class Transition.Property
    const string All
    const string Colors
    const string Opacity
    const string Shadow
    const string Transform
  static class RadioGroup
    const string Default
    const string Indicator
    const string Item
    const string Root
    const string RootHorizontal
  static class Tokens.Radius
    const string Full
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class AspectRatio.Ratio
    const string Photo
    const string Portrait
    const string Square
    const string Video
    const string Wide
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
  static class Layout.Row
    const string Default
    const string InlineCenter
    const string Lg
    const string Md
    const string Sm
    const string SpaceBetween
    const string Xl
    const string Xs
  static class ScrollArea
    const string Bordered
    const string Default
    const string Root
    const string Scrollbar
    const string Thumb
    const string Viewport
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
  static class Separator
    const string Base
    const string Horizontal
    const string Vertical
  static class Tokens.Shadow
    const string Lg
    const string Md
    const string None
    const string Sm
    const string Xl
    const string Xl2
  static class Avatar.Shape
    const string Circle
    const string Square
  static class Skeleton.Shape
    const string Circle
    const string Rectangle
    const string Square
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
  static class Button.Size
    const string Lg
    const string Md
    const string Sm
  static class Toggle.Size
    const string Lg
    const string Md
    const string Sm
  static class Select.Size
    const string Lg
    const string Md
    const string Sm
  static class Progress.Size
    const string Lg
    const string Md
    const string Sm
    const string Xs
  static class Avatar.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xl2
    const string Xs
  static class Skeleton.Size
    const string Lg
    const string Md
    const string Sm
    const string Xl
    const string Xs
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
  static class Slider
    const string Default
    const string Range
    const string Root
    const string RootVertical
    const string Thumb
    const string Track
    const string TrackVertical
  static class Drawer.Snap
    const string Full
    const string Half
    const string Quarter
    const string ThreeQuarter
  static class Layout.Split
    const string Detail
    const string DetailLg
    const string Gapped
    const string Main
    const string Root
    const string Sidebar
    const string SidebarLg
    const string SidebarSm
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
  // How an app's IkonTheme relates to the client's light/dark preference. Not every app wants two themes: a productivity tool should adapt, but a game, an atmospheric experience, or a brand-locked look is designed as ONE palette — and letting a theme toggle pull the platform's dark (or light) defaults in underneath that palette produces a broken half-switched hybrid.
  enum ThemeMode
    Adaptive
    Fixed
  // The canonical theming vocabulary: shadcn-style theme keys and what they commit. Each alias expands to the canonical CSS variables that make its intent real across every consumer (components, focus rings, native clients). This table is the single source of truth — the theme renderer expands aliases through it, the codegen styling tools fan roles out through it, and the docs drift tests lock the published reference tables to it. Collision policy: `primary` as a THEME KEY means brand (the shadcn reading; the Untitled-UI tiered reading only ever existed on the prefixed utility classes, which are untouched). Bare `accent` and `secondary` are deliberately NOT aliases — their shadcn and Ikon meanings genuinely conflict, so they stay unknown-key warnings instead of guessing.
  static class ThemeVocabulary
    // Every accepted alias, keyed by name.
    static IReadOnlyDictionary<string, ThemeVocabulary.Alias> Aliases { get; }
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
  static class Tokens
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
  static class StatCard.TrendVariant
    const string Negative
    const string Neutral
    const string Positive
  // What value shape an alias expects, for docs and tooling.
  enum ThemeVocabulary.ValueKind
    Color
    FontFamily
    Radius
    Duration
    Easing
    Spacing
  static class Separator.Variant
    const string Default
    const string Strong
    const string Subtle
  static class Alert.Variant
    const string Default
    const string Error
    const string Info
    const string Success
    const string Warning
  static class Toggle.Variant
    const string Default
  static class Progress.Variant
    const string Default
    const string Error
    const string Success
    const string Warning
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
  static class ZIndex
    const string Dropdown
    const string Modal
    const string Overlay
    const string Popover
    const string Sticky
    const string Toast
    const string Tooltip
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
