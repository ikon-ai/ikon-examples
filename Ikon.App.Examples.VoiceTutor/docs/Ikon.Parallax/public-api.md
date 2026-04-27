# Ikon.Parallax Public API

namespace Ikon.Parallax
  // Arguments passed to a UI action callback, containing the client context and the deserialized payload.
  sealed class ActionArgs<T>
    ctor()
    // The client context of the user who triggered the action.
    Context ClientContext
    // The deserialized action payload.
    T Value
  // Accumulates profiling samples over multiple render passes, providing aggregate statistics (avg, min, max, p95, p99).
  sealed class ProfileHistory
    // Creates a new history buffer that retains the last render sessions.
    ctor(int maxSamples)
    // Ordered list of distinct measurement names seen across all recorded sessions.
    IReadOnlyList<string> Names { get; }
    // Total number of render sessions recorded (including those evicted from the rolling window).
    long SampleCount { get; }
    // Returns aggregate statistics for a specific named measurement.
    ProfileStats GetStats(string name)
    // Returns a multi-line summary with aggregate stats for total time and each measurement.
    string GetSummary()
    // Returns aggregate statistics for total render time across all sampled sessions.
    ProfileStats GetTotalStats()
    // Clears all accumulated samples and resets the sample count.
    void Reset()
  // Disposable timing scope that records elapsed time into the current when disposed.
  struct ProfileScope : IDisposable
    // Records the elapsed time into the profiling session.
    void Dispose()
  // Records named timing measurements for a single UI render pass.
  sealed class ProfileSession
    ctor()
    // Ordered list of measurement names recorded in this session.
    IReadOnlyList<string> Names { get; }
    // All recorded timings keyed by measurement name.
    IReadOnlyDictionary<string, double> Timings { get; }
    // Total elapsed time for this session in milliseconds.
    double TotalMs { get; }
    // Returns a formatted string with total time and per-measurement breakdown.
    string GetBreakdown()
    // Returns the recorded timing for the given measurement name, or 0 if not found.
    double GetTiming(string name)
    // Stops the session timer and logs the timing breakdown.
    void LogResults()
  // Aggregate profiling statistics for a named measurement or total render time.
  struct ProfileStats : IEquatable<ProfileStats>
    // Aggregate profiling statistics for a named measurement or total render time.
    ctor(double Avg, double Min, double Max, double Median, double P95, double P99)
    double Avg { get; init; }
    double Max { get; init; }
    double Median { get; init; }
    double Min { get; init; }
    double P95 { get; init; }
    double P99 { get; init; }
  // Provides UI render profiling with per-frame timing breakdowns and optional historical statistics.
  static class Profiler
    // Current profiling session for this async context, or null if not profiling.
    static ProfileSession Current { get; }
    // Historical profiling data, or null if history is not enabled.
    static ProfileHistory History { get; }
    // Whether history recording is currently paused.
    static bool IsHistoryPaused { get; }
    // Disables profiling history collection and discards accumulated data.
    static void DisableHistory()
    // Enables profiling history collection, keeping up to render sessions.
    static void EnableHistory(int maxSamples = 1000)
    // Starts a named timing measurement within the current profiling session. Dispose the returned scope to record the elapsed time.
    static ProfileScope Measure(string name)
    // Pauses history recording. New render sessions are not recorded until is called.
    static void PauseHistory()
    // Clears all accumulated profiling history samples.
    static void ResetHistory()
    // Resumes history recording after a pause.
    static void ResumeHistory()
  // Main entry point for the Ikon Parallax reactive UI system. Manages client connections, render cycles, style distribution, and action handling for server-driven UI.
  class UI
    // Creates a new UI instance bound to the given app and theme.
    ctor(IAppBase app, ITheme theme)
    // When true, each render cycle logs timing breakdowns. See for history.
    bool EnableProfiling { get; set; }
    // When true, caches subtrees with unchanged reactive dependencies to skip redundant re-renders.
    bool EnableSubtreeCaching { get; set; }
    // Adds a global CSS block that is sent to all connected clients. Idempotent: identical CSS returns the same style ID.
    string AddGlobalCss(string css)
    // Defines the root UI view tree. Call this in a reactive context to re-render when dependencies change.
    void Root(string[] style = null, Action<UIView> content = null, string styleId = null)
  // Represents a UI view scope for building the component tree. Extension methods on this type provide the component API (e.g. Text, Button, Input).
  class UIView
    // The default icon library name used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }
    // Adds a child node with the given type and props to the current view.
    void AddNode(string type, Dictionary<string, object> props, List<UIViewNode> children = null, string key = null, string[] style = null, string styleId = null, string file = "", int line = 0)
    string CreateAction<T>(Func<ActionArgs<T>, Task> callback)
    // Registers binary data as a payload and returns a reference string for use as an image src.
    string RegisterPayload(byte[] data, string mimeType)
  // Represents a single node in the UI view tree, with identity, props, children, and style information.
  sealed class UIViewNode
    // Creates a new view node with the given type, props, and optional children.
    ctor(string type, Guid viewId, Dictionary<string, object> props, List<UIViewNode> children = null, string key = null, IReadOnlyList<string> styleIds = null, ulong parentPathHash = 0, IReadOnlyCollection<IReadOnlyList<string>> styleIdProps = null, string file = "", int line = 0)
    // Ordered child nodes.
    List<UIViewNode> Children { get; }
    // Lazily computed content signature used for subtree caching and diffing.
    string ContentFingerprint { get; }
    // True when came from an explicit key argument, not from a prop such as value or text.
    bool HasExplicitKey { get; }
    // Stable unique identifier for this node.
    string Id { get; }
    // Precomputed hash of for fast lookups.
    int IdHash { get; }
    // When true, nodes include source file and line markers for debugging.
    static bool IncludeSourceMarkers { get; set; }
    // Component properties passed to the frontend renderer.
    Dictionary<string, object> Props { get; }
    // Source file and line marker for debugging, included only when is true.
    string SourceMarker { get; }
    // Hint string used by the stable ID generator to produce deterministic IDs.
    string StableHint { get; }
    // Resolved Crosswind style class identifiers.
    IReadOnlyList<string> StyleIds { get; }
    // The component type name (e.g. "div", "button").
    string Type { get; }

namespace Ikon.Parallax.Components.Charts
  // Configuration for a chart axis including legend text, tick appearance, and label truncation.
  class AxisConfig
    ctor()
    // Format string for tick labels. For time scales, use d3-time-format tokens (e.g. "%H:%M", "%m/%d %H:%M").
    string Format { get; set; }
    string Legend { get; set; }
    int? LegendOffset { get; set; }
    int? TickPadding { get; set; }
    int? TickRotation { get; set; }
    int? TickSize { get; set; }
    // Number of ticks to display. When set, the axis will show approximately this many evenly-spaced ticks instead of one per data point.
    int? TickValues { get; set; }
    // Truncate tick label text at this character length.
    int? TruncateTickAt { get; set; }
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
    string DomainColor { get; init; }
    ChartTextStyle Legend { get; init; }
    string TickColor { get; init; }
    ChartTextStyle TickLabel { get; init; }
  // Event arguments for chart click interactions.
  class ChartClickArgs
    ctor()
    string Id { get; set; }
    string IndexValue { get; set; }
    string SerieId { get; set; }
    object Value { get; set; }
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
    string LineColor { get; init; }
    string LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Extension methods for rendering interactive chart components (bar, line, pie).
  static class ChartExtensions
    // Renders an interactive bar chart with configurable grouping, layout, axes, and theming.
    static void BarChart(UIView view, string[] style = null, IEnumerable<Dictionary<string, object>> data = null, IEnumerable<string> keys = null, string indexBy = null, BarGroupMode? groupMode = null, BarLayout? layout = null, ScaleType? valueScale = null, ScaleType? indexScale = null, bool? reverse = null, double? minValue = null, double? maxValue = null, double? padding = null, double? innerPadding = null, ChartMargin margin = null, AxisConfig axisTop = null, AxisConfig axisRight = null, AxisConfig axisBottom = null, AxisConfig axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enableLabel = null, int? labelSkipWidth = null, int? labelSkipHeight = null, string labelTextColor = null, IEnumerable<LegendConfig> legends = null, IEnumerable<string> colors = null, ChartColorScheme? colorScheme = null, ChartTheme theme = null, string borderColor = null, double? borderRadius = null, double? borderWidth = null, bool? isInteractive = null, Func<ChartClickArgs, Task> onClick = null, string styleId = null, string key = null, string file = "", int line = 0)
    // Renders an interactive line chart with configurable curves, points, areas, and crosshairs.
    static void LineChart(UIView view, string[] style = null, IEnumerable<LineChartSeries> data = null, ScaleType? xScaleType = null, ScaleType? yScaleType = null, double? yScaleMin = null, double? yScaleMax = null, bool? yScaleStacked = null, ChartMargin margin = null, AxisConfig axisTop = null, AxisConfig axisRight = null, AxisConfig axisBottom = null, AxisConfig axisLeft = null, bool? enableGridX = null, bool? enableGridY = null, bool? enablePoints = null, int? pointSize = null, string pointColor = null, string pointBorderColor = null, int? pointBorderWidth = null, bool? enableArea = null, double? areaOpacity = null, double? areaBaselineValue = null, bool? enableCrosshair = null, CrosshairType? crosshairType = null, LineCurve? curve = null, IEnumerable<LegendConfig> legends = null, IEnumerable<string> colors = null, ChartColorScheme? colorScheme = null, ChartTheme theme = null, double? lineWidth = null, bool? isInteractive = null, bool? useMesh = null, Func<ChartClickArgs, Task> onClick = null, string styleId = null, string key = null, string file = "", int line = 0)
    // Renders an interactive pie/donut chart with configurable arc labels, link labels, and legends.
    static void PieChart(UIView view, string[] style = null, IEnumerable<PieChartDatum> data = null, double? innerRadius = null, double? padAngle = null, double? cornerRadius = null, double? startAngle = null, double? endAngle = null, bool? sortByValue = null, ChartMargin margin = null, bool? enableArcLabels = null, string arcLabelsTextColor = null, double? arcLabelsSkipAngle = null, bool? enableArcLinkLabels = null, string arcLinkLabelsTextColor = null, double? arcLinkLabelsSkipAngle = null, double? arcLinkLabelsThickness = null, string arcLinkLabelsColor = null, double? activeOuterRadiusOffset = null, IEnumerable<LegendConfig> legends = null, IEnumerable<string> colors = null, ChartColorScheme? colorScheme = null, ChartTheme theme = null, string borderColor = null, double? borderWidth = null, bool? isInteractive = null, Func<ChartClickArgs, Task> onClick = null, string styleId = null, string key = null, string file = "", int line = 0)
  // Styling for chart grid lines.
  class ChartGridStyle : IEquatable<ChartGridStyle>
    ctor()
    string LineColor { get; init; }
    string LineDashArray { get; init; }
    int? LineWidth { get; init; }
  // Styling for chart data labels.
  class ChartLabelsStyle : IEquatable<ChartLabelsStyle>
    ctor()
    ChartTextStyle Text { get; init; }
  // Styling for chart legend text and title.
  class ChartLegendStyle : IEquatable<ChartLegendStyle>
    ctor()
    ChartTextStyle Text { get; init; }
    ChartTextStyle Title { get; init; }
  // Margin configuration for chart containers.
  class ChartMargin
    ctor()
    int? Bottom { get; set; }
    int? Left { get; set; }
    int? Right { get; set; }
    int? Top { get; set; }
  // Text styling for chart elements.
  class ChartTextStyle : IEquatable<ChartTextStyle>
    ctor()
    string Color { get; init; }
    string FontFamily { get; init; }
    int? FontSize { get; init; }
  // Complete theme configuration for chart components, combining all styling aspects.
  class ChartTheme : IEquatable<ChartTheme>
    ctor()
    ChartAxisStyle Axis { get; init; }
    ChartColorScheme? ColorScheme { get; init; }
    string[] Colors { get; init; }
    ChartCrosshairStyle Crosshair { get; init; }
    ChartGridStyle Grid { get; init; }
    ChartLabelsStyle Labels { get; init; }
    ChartLegendStyle Legends { get; init; }
    ChartTextStyle Text { get; init; }
    ChartTooltipStyle Tooltip { get; init; }
  // Built-in chart theme presets for light and dark backgrounds.
  static class ChartThemes
    // Chart theme optimized for dark backgrounds with muted but saturated series colors.
    static ChartTheme DefaultDark { get; }
    // Chart theme optimized for light backgrounds with soft, pastel-like series colors.
    static ChartTheme DefaultLight { get; }
  // Styling for chart tooltips.
  class ChartTooltipStyle : IEquatable<ChartTooltipStyle>
    ctor()
    string BackgroundColor { get; init; }
    string BorderColor { get; init; }
    int? BorderRadius { get; init; }
    ChartTextStyle Text { get; init; }
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
  // Configuration for a chart legend including positioning, layout direction, and item sizing.
  class LegendConfig
    ctor()
    string Anchor { get; set; }
    string Direction { get; set; }
    int? ItemHeight { get; set; }
    int? ItemWidth { get; set; }
    int? ItemsSpacing { get; set; }
    int? SymbolSize { get; set; }
    int? TranslateX { get; set; }
    int? TranslateY { get; set; }
  // A single data point in a line chart series.
  class LineChartPoint
    object X { get; set; }
    object Y { get; set; }
  // A named data series for a line chart, containing an ordered collection of points.
  class LineChartSeries
    string Color { get; set; }
    IEnumerable<LineChartPoint> Data { get; set; }
    string Id { get; set; }
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
  class PieChartDatum
    string Color { get; set; }
    string Id { get; set; }
    string Label { get; set; }
    double Value { get; set; }
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
    string ActionId { get; init; }
    // Action buttons for "actions" type cells.
    CellAction[] Actions { get; init; }
    // When true, the cell's interactive element is disabled.
    bool? Disabled { get; init; }
    // Button label for action cells.
    string Label { get; init; }
    // Crosswind style classes for the cell.
    string[] Style { get; init; }
    // Cell type: "text", "badge", "action", "actions", or "checkbox".
    string Type { get; init; }
    // Display value or checkbox state ("true"/"false").
    string Value { get; init; }
    // Visual variant for badge cells.
    string Variant { get; init; }
    // Creates an action button cell.
    static Cell Action(string label, string actionId, string[] style = null)
    // Creates a cell containing multiple action buttons.
    static Cell ActionGroup(CellAction[] actions)
    // Creates a badge cell with an optional variant.
    static Cell Badge(string value, string variant = null, string[] style = null)
    // Creates a checkbox cell.
    static Cell Checkbox(bool checked, string actionId, string[] style = null, bool disabled = false)
    // Creates a text cell.
    static Cell Text(string value, string[] style = null)
  // An action button that can be displayed within a data table cell.
  class CellAction : IEquatable<CellAction>
    // An action button that can be displayed within a data table cell.
    ctor(string Label, string ActionId, string[] Style = null, string Icon = null)
    string ActionId { get; init; }
    string Icon { get; init; }
    string Label { get; init; }
    string[] Style { get; init; }
  // Defines a column in a data table including header text, width, and alignment.
  class DataTableColumn : IEquatable<DataTableColumn>
    // Defines a column in a data table including header text, width, and alignment.
    ctor(string Header, string Width = null, int Flex = 0, ColumnAlign Align = Left, string MinWidth = null, bool Wrap = false)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string MinWidth { get; init; }
    string Width { get; init; }
    bool Wrap { get; init; }
  // Extension methods for rendering paginated data tables.
  static class DataTableExtensions
    // Renders a paginated data table with configurable columns, rows, actions, and styling.
    static void DataTable(UIView view, DataTableColumn[] columns, DataTableRow[] rows, int totalCount, int pageIndex, int pageSize, Func<int, Task> onPageChange = null, Func<string, Task> onRowClick = null, Func<string, Task> onActionClick = null, Action<UIView> emptyContent = null, int[] columnWidths = null, Func<string, Task> onColumnResize = null, string[] style = null, string[] headerStyle = null, string[] rowStyle = null, string[] cellStyle = null, string[] headerCellStyle = null, string[] dataCellStyle = null, string[] paginationStyle = null, string[] paginationButtonStyle = null, string[] pageNumberStyle = null, string[] pageNumberActiveStyle = null, string[] emptyStyle = null, string[] actionButtonStyle = null, string[] resizeHandleStyle = null, string[] tooltipStyle = null, string prevLabel = null, string nextLabel = null, string pageLabel = null, string key = null, string file = "", int line = 0)
  // A single row in a data table, identified by a unique ID and containing an array of cells.
  class DataTableRow : IEquatable<DataTableRow>
    // A single row in a data table, identified by a unique ID and containing an array of cells.
    ctor(string Id, Cell[] Cells)
    Cell[] Cells { get; init; }
    string Id { get; init; }

namespace Ikon.Parallax.Components.ImageEditor
  // Extension methods for the image editor canvas component.
  static class ImageEditorExtensions
    // Canvas for editing images with brush and eraser tools.
    static void ImageEditorCanvas(UIView view, string[] style = null, string src = null, int? brushWidth = null, string brushColor = null, string tool = null, double? zoom = null, Func<ImageEditorSaveArgs, Task> onSave = null, Func<ImageEditorHistoryArgs, Task> onHistoryChange = null, int? triggerSave = null, int? triggerUndo = null, int? triggerRedo = null, string styleId = null, string key = null, string file = "", int line = 0)
  // Event args for when the undo/redo history state changes.
  sealed class ImageEditorHistoryArgs : IEquatable<ImageEditorHistoryArgs>
    // Event args for when the undo/redo history state changes.
    ctor(bool CanUndo, bool CanRedo)
    bool CanRedo { get; init; }
    bool CanUndo { get; init; }
  // Event args for when the image editor saves the edited image.
  sealed class ImageEditorSaveArgs : IEquatable<ImageEditorSaveArgs>
    // Event args for when the image editor saves the edited image.
    ctor(string ImageData)
    string ImageData { get; init; }

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
    Dictionary<string, JsonElement> Properties { get; init; }
    // Type-safe accessor for the event's custom properties.
    RiveEventProperties Props { get; }
    // Target identifier for the event.
    string Target { get; init; }
    // The Rive event type identifier.
    int? Type { get; init; }
    // URL associated with the event, if any.
    string Url { get; init; }
  // Helper class for accessing Rive event properties with type-safe methods.
  sealed class RiveEventProperties
    // Helper class for accessing Rive event properties with type-safe methods.
    ctor(Dictionary<string, JsonElement> properties)
    // Gets a boolean property value, or if not found.
    bool GetBool(string key, bool defaultValue = false)
    // Gets a double property value, or if not found.
    double GetDouble(string key, double defaultValue = 0)
    // Gets an integer property value, or if not found.
    int GetInt(string key, int defaultValue = 0)
    // Gets a string property value, or if not found.
    string GetString(string key, string defaultValue = "")
  // Extension methods for Rive animation components.
  static class RiveExtensions
    // Canvas for rendering Rive animations with state machine support.
    static void RiveCanvas(UIView view, string[] style = null, string source = null, IEnumerable<string> stateMachines = null, RiveViewModel viewModel = null, IEnumerable<RiveTrigger> triggers = null, Func<RiveEventData, Task> onEvent = null, RiveFit? layoutFit = null, RiveAlignment? layoutAlignment = null, bool? autoplay = null, bool? useOffscreenRenderer = null, bool? autoBind = null, bool? enableMultiTouch = null, bool? dispatchPointerExit = null, bool? isTouchScrollEnabled = null, bool? shouldDisableRiveListeners = null, IEnumerable<RiveKeyboardBinding> keyboardBindings = null, string backgroundColor = null, string width = null, string height = null, string styleId = null, string key = null, string file = "", int line = 0)
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
    // Represents a trigger that can be fired in a Rive animation. Calling Fire() increments the sequence and triggers a UI re-render.
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
    RiveViewModel String(string name, string value)

namespace Ikon.Parallax.Components.Standard
  // Extension methods for accessibility components.
  static class AccessibilityExtensions
    // Wraps an icon with accessible label for screen readers.
    static void AccessibleIcon(UIView view, string[] style = null, string label = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Hides content visually while keeping it accessible to screen readers.
    static void VisuallyHidden(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // Base event returned from a client-side action, indicating the action type and whether it succeeded.
  class ActionEvent : IEquatable<ActionEvent>
    // Base event returned from a client-side action, indicating the action type and whether it succeeded.
    ctor(string ActionType, bool Success)
    string ActionType { get; init; }
    bool Success { get; init; }
  // JSON converter that deserializes into the correct derived type based on the ActionType field.
  class ActionEventConverter : JsonConverter<ActionEvent>
    ctor()
    override ActionEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    override void Write(Utf8JsonWriter writer, ActionEvent value, JsonSerializerOptions options)
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
    ShowNotification
  // Base class for client-side action configuration.
  abstract class ActionOptions : IEquatable<ActionOptions>
  // Represents activation mode for Tabs.
  enum ActivationMode
    Automatic
    Manual
  // Represents alignment for overlay positioning.
  enum Align
    Start
    Center
    End
  // Extension methods for Calendar and DatePicker components.
  static class CalendarExtensions
    // Month-grid date selector. Renders a single month with day cells. Dates are ISO yyyy-MM-dd strings.
    static void Calendar(UIView view, string[] style = null, string value = null, string defaultValue = null, string month = null, string defaultMonth = null, string minDate = null, string maxDate = null, IReadOnlyList<string> disabledDates = null, WeekStart weekStart = Monday, string locale = null, bool? disabled = null, string[] headerStyle = null, string[] weekdayStyle = null, string[] dayStyle = null, string[] daySelectedStyle = null, string[] dayTodayStyle = null, string[] dayOutsideStyle = null, string[] dayDisabledStyle = null, string[] navButtonStyle = null, string[] titleStyle = null, string[] gridStyle = null, string[] rowStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onMonthChange = null, string file = "", int line = 0)
    // Button that opens a popover containing a .
    static void DatePicker(UIView view, string[] style = null, string value = null, string defaultValue = null, string placeholder = null, string format = null, string minDate = null, string maxDate = null, IReadOnlyList<string> disabledDates = null, WeekStart weekStart = Monday, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[] triggerStyle = null, string[] contentStyle = null, string[] calendarStyle = null, string[] headerStyle = null, string[] weekdayStyle = null, string[] dayStyle = null, string[] daySelectedStyle = null, string[] dayTodayStyle = null, string[] dayOutsideStyle = null, string[] dayDisabledStyle = null, string[] navButtonStyle = null, string[] titleStyle = null, string[] gridStyle = null, string[] rowStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
  // Options for capturing an image from the client's camera.
  sealed class CaptureImageActionOptions : ActionOptions, IEquatable<CaptureImageActionOptions>
    ctor()
    // Hardware constraints for camera selection.
    CaptureImageConstraints Constraints { get; init; }
    // Output image format.
    ClientImageCaptureFormat? Format { get; init; }
    // Desired image height in pixels.
    int? Height { get; init; }
    // Image quality (0.0 to 1.0) for lossy formats.
    double? Quality { get; init; }
    // Desired image width in pixels.
    int? Width { get; init; }
  // Hardware constraints for image capture.
  sealed class CaptureImageConstraints : IEquatable<CaptureImageConstraints>
    ctor()
    // Preferred camera device ID.
    string DeviceId { get; init; }
  // Alignment of slides relative to the carousel viewport.
  enum CarouselAlign
    Start
    Center
    End
  // Extension methods for Carousel components.
  static class CarouselExtensions
    // Horizontal or vertical carousel with optional navigation arrows and indicator dots.
    static void Carousel(UIView view, string[] style = null, int? index = null, int? defaultIndex = null, Orientation orientation = Horizontal, CarouselAlign align = Start, bool? loop = null, int? autoPlayMs = null, IEnumerable<CarouselSlideItem> slides = null, bool? showArrows = null, bool? showIndicators = null, string previousLabel = null, string nextLabel = null, string previousIconName = null, string nextIconName = null, string[] rootStyle = null, string[] viewportStyle = null, string[] slideStyle = null, string[] previousStyle = null, string[] nextStyle = null, string[] indicatorsStyle = null, string[] indicatorStyle = null, string[] indicatorActiveStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, Func<double, Task> onIndexChange = null, string file = "", int line = 0)
    // A single slide inside a . Use when rendering slides manually.
    static void Slide(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // Declarative slide definition for .
  sealed class CarouselSlideItem : IEquatable<CarouselSlideItem>
    // Declarative slide definition for .
    ctor(Action<UIView> Content, string Key = null)
    // Builder function for rendering the slide.
    Action<UIView> Content { get; init; }
    // Optional stable key used for diffing.
    string Key { get; init; }
  // Represents the checked state for checkbox-like components.
  enum CheckedState
    Unchecked
    Checked
    Indeterminate
  // Extension methods for the CodeEditor component.
  static class CodeEditorExtensions
    // Monospace code editor with an optional line-number gutter.
    static void CodeEditor(UIView view, string value = null, string defaultValue = null, string language = null, string placeholder = null, bool? readOnly = null, bool? disabled = null, bool? showLineNumbers = null, int? tabSize = null, bool? insertSpaces = null, bool? wrap = null, int? minRows = null, int? maxRows = null, string[] style = null, string[] gutterStyle = null, string[] contentStyle = null, string[] languageBadgeStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onSubmit = null, string file = "", int line = 0)
  // Represents collision detection strategy for @dnd-kit.
  enum CollisionDetection
    ClosestCenter
    ClosestCorners
    RectIntersection
    PointerWithin
  // Output string format for .
  enum ColorFormat
    Hex
    Rgb
    Hsl
  // Extension methods for ColorPicker components.
  static class ColorPickerExtensions
    // Swatch-triggered color picker with hue slider, saturation/lightness square, and hex input.
    static void ColorPicker(UIView view, string[] style = null, string value = null, string defaultValue = null, ColorFormat format = Hex, bool? showAlpha = null, IReadOnlyList<string> presets = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string[] triggerStyle = null, string[] contentStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onValueCommit = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
  // Horizontal alignment for a content grid or data table column.
  enum ColumnAlign
    Left
    Center
    Right
  // Event returned from a contact picker action with the selected contacts.
  sealed class ContactsActionEvent : ActionEvent, IEquatable<ContactsActionEvent>
    // Event returned from a contact picker action with the selected contacts.
    ctor(bool Success, IReadOnlyList<ClientContact> Contacts)
    IReadOnlyList<ClientContact> Contacts { get; init; }
  // Extension methods for container components.
  static class ContainerExtensions
    // Generic container element.
    static void Box(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, Action<UIView> content = null, string file = "", int line = 0)
    // Box — positional (style, children) overload. Models reach for view.Box([style], v => {...}) with the lambda as the 2nd positional; without this overload it tries to bind to styleId (string?) and trips CS1660. The lambda parameter is named children (not content) so existing callers that use content: by name unambiguously match the original.
    static void Box(UIView view, string[] style, Action<UIView> children, string file = "", int line = 0)
    // Container with vertical flexbox layout (flex-col).
    static void Column(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
    // Column — positional (style, children) overload.
    static void Column(UIView view, string[] style, Action<UIView> children, string file = "", int line = 0)
    // Container with flexbox layout enabled.
    static void Flex(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
    // Container with CSS grid layout enabled.
    static void Grid(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
    // Grid — positional (style, children) overload.
    static void Grid(UIView view, string[] style, Action<UIView> children, string file = "", int line = 0)
    // Absolutely positioned layer within a Stack container.
    static void Layer(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
    // Container with horizontal flexbox layout (flex-row).
    static void Row(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
    // Row — positional (style, children) overload (see ).
    static void Row(UIView view, string[] style, Action<UIView> children, string file = "", int line = 0)
    // Container for layering children on top of each other. Use with Layer components as children.
    static void Stack(UIView view, string[] style = null, string styleId = null, string key = null, Action<UIView> content = null, string file = "", int line = 0)
  // Defines a column in a content grid including optional header, width, flex, and alignment.
  class ContentGridColumn : IEquatable<ContentGridColumn>
    // Defines a column in a content grid including optional header, width, flex, and alignment.
    ctor(string Header, string Width = null, int Flex = 0, ColumnAlign Align = Left)
    ColumnAlign Align { get; init; }
    int Flex { get; init; }
    string Header { get; init; }
    string Width { get; init; }
  // Extension methods for CSS grid-based content layout.
  static class ContentGridExtensions
    // Renders a CSS grid layout with configurable columns, optional headers, and child content.
    static void ContentGrid(UIView view, ContentGridColumn[] columns, Action<UIView> content = null, string[] style = null, string[] headerStyle = null, string key = null, string file = "", int line = 0)
  // Options for copying text to the clipboard.
  sealed class CopyToClipboardActionOptions : ActionOptions, IEquatable<CopyToClipboardActionOptions>
    // The text to copy.
    string Text { get; init; }
  // Extension methods for core UI components including buttons, toggles, text inputs, dialogs, and typography.
  static class CoreExtensions
    // Button that triggers a client-side action (e.g., clipboard, download). Supports both text mode and icon mode. In text mode (content is null or label is null), label is displayed as visible text. In icon mode (content and label are both provided), label becomes the accessible aria-label and content is displayed.
    static void ActionButton(UIView view, string[] style = null, ActionKind action = Unknown, string label = null, ActionOptions options = null, bool? disabled = null, string className = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<ActionEvent, Task> onActionComplete = null, Action<UIView> content = null, string file = "", int line = 0)
    // Clickable button that triggers an action. Supports both text mode and icon mode. In text mode (content is null), text is displayed as visible button text. In icon mode (content is provided), text becomes the accessible aria-label and content is displayed.
    static void Button(UIView view, string[] style = null, string text = null, string label = null, bool? disabled = null, string href = null, string type = null, string target = null, string rel = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, string icon = null, string iconPosition = null, Action<UIView> content = null, string file = "", int line = 0)
    // Button — positional-text-first overload. Same rationale as the matching Text overload — avoids CS1744 when models write view.Button("Sign in", onClick: …). First parameter is named buttonText to avoid ambiguity with callers using Button(text: "...") by name.
    static void Button(UIView view, string buttonText, string[] style = null, string label = null, bool? disabled = null, string href = null, string type = null, string target = null, string rel = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, string icon = null, string iconPosition = null, Action<UIView> content = null, string file = "", int line = 0)
    // Semantic heading element for titles and section headers.
    static void Heading(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Renders an icon from an icon library.
    static void Icon(UIView view, string[] style = null, string name = null, string library = null, string className = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Renders markdown content with formatting support.
    static void Markdown(UIView view, string[] style = null, string content = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Text element for displaying content.
    static void Text(UIView view, string[] style = null, string text = null, string href = null, string target = null, string rel = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Text element — positional-text-first overload. Models trained on shadcn / Radix / React conventions reach for view.Text("Hello", style: ["text-xl"]) rather than the view.Text(["text-xl"], "Hello") ordering. Without this overload, the positional string argument fails to bind to the original signature's first parameter (string[]? style), producing CS1744 / CS1503 — the most common compile error in the codegen benchmark. Parameter is named textContent (not text) to avoid ambiguity with existing callers that use Text(text: "...") by name.
    static void Text(UIView view, string textContent, string[] style = null, string href = null, string target = null, string rel = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Single toggle button.
    static void Toggle(UIView view, string[] style = null, bool? pressed = null, bool? defaultPressed = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool, Task> onPressedChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Item within a toggle group.
    static void ToggleGroupItem(UIView view, string[] style = null, string value = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggle group with multiple selection.
    static void ToggleGroupMultiple(UIView view, string[] style = null, IReadOnlyList<string> value = null, IReadOnlyList<string> defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<IReadOnlyList<string>, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggle group with single selection.
    static void ToggleGroupSingle(UIView view, string[] style = null, string value = null, string defaultValue = null, bool? rovingFocus = true, bool loop = true, Orientation orientation = Horizontal, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
  // Represents the text direction for DirectionProvider.
  enum Dir
    Ltr
    Rtl
  // Extension methods for Accordion and Collapsible components.
  static class DisclosureExtensions
    // Content for an accordion item, collapsed or expanded.
    static void AccordionContent(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Wraps an AccordionTrigger.
    static void AccordionHeader(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Container for an accordion item.
    static void AccordionItem(UIView view, string[] style = null, string value = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Accordion with multiple items open at a time.
    static void AccordionMultiple(UIView view, string[] style = null, IReadOnlyList<string> value = null, IReadOnlyList<string> defaultValue = null, Orientation orientation = Vertical, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<IReadOnlyList<string>, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Accordion with single item open at a time.
    static void AccordionSingle(UIView view, string[] style = null, string value = null, string defaultValue = null, bool? collapsible = null, Orientation orientation = Vertical, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggles the collapsed state of an accordion item.
    static void AccordionTrigger(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Expandable/collapsible container.
    static void Collapsible(UIView view, string[] style = null, bool? open = null, bool? defaultOpen = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool, Task> onOpenChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Content that is shown or hidden.
    static void CollapsibleContent(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggles the collapsed state.
    static void CollapsibleTrigger(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // Options for downloading a file to the client.
  sealed class DownloadFileActionOptions : ActionOptions, IEquatable<DownloadFileActionOptions>
    ctor()
    // Binary data to download. When set, Url is auto-generated as a data URL.
    byte[] Data { get; init; }
    // Suggested filename for the downloaded file.
    string Filename { get; init; }
    // MIME type for binary data (e.g. "image/png"). Required when Data is set.
    string MimeType { get; init; }
    // URL to download. Can be a regular URL or a data URL. If Data is provided, this is auto-generated from the binary data.
    string Url { get; init; }
  // Extension methods for drag and drop components.
  static class DragAndDropExtensions
    // Root context for drag and drop operations.
    static void DndContext(UIView view, string[] style = null, CollisionDetection collisionDetection = ClosestCenter, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<DragStartArgs, Task> onDragStart = null, Func<DragMoveArgs, Task> onDragMove = null, Func<DragOverArgs, Task> onDragOver = null, Func<DragEndArgs, Task> onDragEnd = null, Func<Task> onDragCancel = null, Action<UIView> content = null, string file = "", int line = 0)
    // Overlay shown while dragging.
    static void DragOverlay(UIView view, string[] style = null, bool? dropAnimation = true, string activeDragId = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Element that can be dragged.
    static void Draggable(UIView view, string[] style = null, string id = null, bool? disabled = null, bool? hideOnDrag = null, object data = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Drop target area.
    static void Droppable(UIView view, string[] style = null, string id = null, bool? disabled = null, object data = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Context for sortable list operations.
    static void SortableContext(UIView view, string[] style = null, IReadOnlyList<string> items = null, SortStrategy strategy = VerticalList, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Sortable item within a SortableContext.
    static void SortableItem(UIView view, string[] style = null, string id = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // SortableList component that auto-handles reordering.
    static void SortableList(UIView view, IReadOnlyList<string> items = null, SortStrategy strategy = VerticalList, CollisionDetection collisionDetection = ClosestCenter, Func<SortableReorderArgs, Task> onReorder = null, Func<DragStartArgs, Task> onDragStart = null, Action<UIView, string> itemContent = null, string[] listStyle = null, string[] itemStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
  // Event args for drag cancel in @dnd-kit.
  sealed class DragCancelArgs : IEquatable<DragCancelArgs>
    // Event args for drag cancel in @dnd-kit.
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for drag end in @dnd-kit.
  sealed class DragEndArgs : IEquatable<DragEndArgs>
    // Event args for drag end in @dnd-kit.
    ctor(string ActiveId, string OverId)
    string ActiveId { get; init; }
    string OverId { get; init; }
  // Event args for drag move in @dnd-kit.
  sealed class DragMoveArgs : IEquatable<DragMoveArgs>
    // Event args for drag move in @dnd-kit.
    ctor(string ActiveId, double DeltaX, double DeltaY)
    string ActiveId { get; init; }
    double DeltaX { get; init; }
    double DeltaY { get; init; }
  // Event args for drag over in @dnd-kit.
  sealed class DragOverArgs : IEquatable<DragOverArgs>
    // Event args for drag over in @dnd-kit.
    ctor(string ActiveId, string OverId)
    string ActiveId { get; init; }
    string OverId { get; init; }
  // Event args for drag start in @dnd-kit.
  sealed class DragStartArgs : IEquatable<DragStartArgs>
    // Event args for drag start in @dnd-kit.
    ctor(string ActiveId)
    string ActiveId { get; init; }
  // Event args for escape key down events on overlays.
  sealed class EscapeKeyDownArgs : IEquatable<EscapeKeyDownArgs>
    // Event args for escape key down events on overlays.
    ctor()
  // Hint used by to preload the slide's primary media asset.
  enum FeedMediaKind
    None
    Image
    Video
    VideoFull
  // Extension methods for the FeedScroller component — a vertically-snapping, full-viewport feed optimized for media-heavy content (TikTok / Reels / Shorts-style).
  static class FeedScrollerExtensions
    // Renders a TikTok-style vertical feed: each slide occupies the viewport and snaps into place.
    static void FeedScroller(UIView view, IEnumerable<FeedSlide> slides, int? activeIndex = null, int? defaultActiveIndex = null, int preloadAhead = 2, int preloadBehind = 1, bool? autoPlay = null, bool? muted = null, bool? loop = null, int scrollEndThreshold = 2, string[] style = null, string[] slideStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<double, Task> onActiveChange = null, Func<double, Task> onScrollNearEnd = null, Func<bool, Task> onMuteChange = null, string file = "", int line = 0)
    // A single slide inside a . Use when rendering slides manually rather than via the declarative API.
    static void FeedSlide(UIView view, int index, string[] style = null, FeedMediaKind mediaKind = None, string mediaUrl = null, string mediaPoster = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // A single slide in a .
  sealed class FeedSlide : IEquatable<FeedSlide>
    // A single slide in a .
    ctor(Action<UIView> Content, string Key = null, FeedMediaKind MediaKind = None, string MediaUrl = null, string MediaPoster = null)
    // Builder invoked to render the slide. Only slides inside the render window are realized.
    Action<UIView> Content { get; init; }
    // Stable key used for diffing and preload identity. Defaults to slide index.
    string Key { get; init; }
    // Kind of media the slide needs preloaded.
    FeedMediaKind MediaKind { get; init; }
    // Optional poster image URL for video slides.
    string MediaPoster { get; init; }
    // URL of the media asset matching .
    string MediaUrl { get; init; }
  // Extension methods for file picker components. Unlike , a FilePicker only opens the native file picker and reports selected file metadata to the server — it does not transfer bytes. The picked File handles are cached on the client and uploaded later by a rendered with a matching seedSelectionIds prop.
  static class FilePickerExtensions
    // Native file picker. Emits once per selected file with its metadata (name, mime, size, client-generated selection id). The File bytes stay on the client and are not transferred until a FileUpload with matching seedSelectionIds is mounted.
    static void FilePicker(UIView view, string[] style = null, string[] accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<FilePickerSelectedArgs, Task> onFileSelected = null, Action<UIView> content = null, string file = "", int line = 0)
  // Metadata for a file chosen in a . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
  sealed class FilePickerSelectedArgs : IEquatable<FilePickerSelectedArgs>
    // Metadata for a file chosen in a . The file bytes are held on the client until an upload is triggered later via a FileUpload with matching seedSelectionIds.
    ctor(string SelectionId, string FileName, string MimeType, long Size)
    string FileName { get; init; }
    string MimeType { get; init; }
    string SelectionId { get; init; }
    long Size { get; init; }
  // Extension methods for file upload components.
  static class FileUploadExtensions
    // File upload component with explicit upload area, button click, drag-drop, and paste support.
    static void FileUpload(UIView view, string[] style = null, string[] accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, string capture = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>> onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadStartResult>> onUploadStart = null, Func<FileUploadProgressArgs, Task> onUploadProgress = null, Func<FileUploadCompleteArgs, Task> onUploadComplete = null, Func<FileUploadErrorArgs, Task> onUploadError = null, Func<FileUploadChunkArgs, Task> onChunkReceived = null, string[] seedSelectionIds = null, Action<UIView> content = null, string file = "", int line = 0)
    // Wrapper component that adds file upload capability (drag-drop + paste) to any content. Children define the visual appearance.
    static void FileUploadZone(UIView view, string[] accept = null, bool? multiple = null, long? maxFileSize = null, bool? disabled = null, bool? allowPaste = null, Func<FileUploadPreStartArgs, Task<FileUploadPreStartResult>> onUploadPreStart = null, Func<FileUploadStartArgs, Task<FileUploadStartResult>> onUploadStart = null, Func<FileUploadProgressArgs, Task> onUploadProgress = null, Func<FileUploadCompleteArgs, Task> onUploadComplete = null, Func<FileUploadErrorArgs, Task> onUploadError = null, Func<FileUploadChunkArgs, Task> onChunkReceived = null, Func<bool, Task> onDragActiveChange = null, Action<UIView> content = null, string[] zoneStyle = null, string[] activeStyle = null, string activeStyleId = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
  // Extension methods for focus hint management.
  static class FocusHintExtensions
    // Requests focus attention for a UI element, typically for accessibility announcements.
    static void FocusHint(UIView view, FocusHintProps props, string key = null, Guid? targetViewId = null)
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
    // Event args for focus outside events on overlays.
    ctor(string TargetId)
    string TargetId { get; init; }
  // Priority level for focus hint announcements, matching ARIA live region politeness.
  enum FocusPriority
    Polite
    Assertive
  // Extension methods for Form, Checkbox, RadioGroup, Switch, Slider, and Label components.
  static class FormExtensions
    // Checkbox control with simple boolean state. For tri-state support (indeterminate), use the CheckedState overload.
    static void Checkbox(UIView view, string[] style = null, bool? isChecked = null, bool? defaultChecked = null, bool? required = null, bool? disabled = null, string name = null, string value = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool, Task> onCheckedChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Checkbox control with tri-state support (checked, unchecked, indeterminate).
    static void Checkbox(UIView view, string[] style = null, CheckedState? checkedState = null, CheckedState? defaultCheckedState = null, bool? required = null, bool? disabled = null, string name = null, string value = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<CheckedState, Task> onCheckedStateChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Visual indicator for the checkbox state.
    static void CheckboxIndicator(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Form container with validation support.
    static void Form(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClearServerErrors = null, Action<UIView> content = null, string file = "", int line = 0)
    // Wraps the input control.
    static void FormControl(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Container for a form field with label and validation.
    static void FormField(UIView view, string[] style = null, string name = null, bool? serverInvalid = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Label for a form field.
    static void FormLabel(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Validation message for a form field.
    static void FormMessage(UIView view, string[] style = null, FormMessageMatch? match = null, bool? forceMatch = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Submit button for the form.
    static void FormSubmit(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Accessible label for form controls.
    static void Label(UIView view, string[] style = null, string htmlFor = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Container for radio buttons.
    static void RadioGroup(UIView view, string[] style = null, string value = null, string defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string name = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Visual indicator for the selected radio.
    static void RadioGroupIndicator(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Individual radio button.
    static void RadioGroupItem(UIView view, string[] style = null, string value = null, bool? disabled = null, bool? required = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Range slider control.
    static void Slider(UIView view, string[] style = null, IReadOnlyList<double> value = null, IReadOnlyList<double> defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string name = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<IReadOnlyList<double>, Task> onValueChange = null, Func<IReadOnlyList<double>, Task> onValueCommit = null, Action<UIView> content = null, string file = "", int line = 0)
    // Filled range portion of the slider.
    static void SliderRange(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Draggable thumb on the slider.
    static void SliderThumb(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Track for the slider.
    static void SliderTrack(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggle switch control.
    static void Switch(UIView view, string[] style = null, bool? isChecked = null, bool? defaultChecked = null, bool? required = null, bool? disabled = null, string name = null, string value = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool, Task> onCheckedChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // The thumb that moves when the switch is toggled.
    static void SwitchThumb(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
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
  // Hour display format for .
  enum HourFormat
    Hour24
    Hour12
  // Event returned from an image capture action with the captured image data.
  sealed class ImageCaptureActionEvent : ActionEvent, IEquatable<ImageCaptureActionEvent>
    // Event returned from an image capture action with the captured image data.
    ctor(bool Success, string Mime, int Width, int Height, string Data)
    string Data { get; init; }
    int Height { get; init; }
    string Mime { get; init; }
    int Width { get; init; }
  // Extension methods for image and avatar components.
  static class ImageExtensions
    // Avatar container with image and fallback.
    static void Avatar(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Fallback content shown when image fails to load.
    static void AvatarFallback(UIView view, string[] style = null, int? delayMs = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Image element for the avatar.
    static void AvatarImage(UIView view, string[] style = null, string src = null, string alt = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onLoadingStatusChange = null, string file = "", int line = 0)
    // Image element.
    static void Image(UIView view, string[] style = null, string src = null, AssetUri? assetUri = null, string alt = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, string file = "", int line = 0)
    // Image element with binary data payload.
    static void Image(UIView view, string[] style = null, byte[] data = null, string mimeType = null, string alt = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, string file = "", int line = 0)
  // Extension methods for input components (TextField, TextArea, OTP, Password).
  static class InputExtensions
    // One-time password input field.
    static void OtpField(UIView view, string[] style = null, string value = null, int? maxLength = null, bool autoSubmit = false, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<Task> onAutoSubmit = null, Action<UIView> content = null, string file = "", int line = 0)
    // Individual input slot for OTP.
    static void OtpFieldInput(UIView view, string[] style = null, int index = 0, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Password input with visibility toggle.
    static void PasswordToggleField(UIView view, string[] style = null, bool? visible = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool, Task> onVisibilityChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Icon that changes based on visibility state.
    static void PasswordToggleFieldIcon(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> visibleIcon = null, Action<UIView> hiddenIcon = null, string file = "", int line = 0)
    // The password input element.
    static void PasswordToggleFieldInput(UIView view, string[] style = null, string autoComplete = null, string placeholder = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Button to toggle password visibility.
    static void PasswordToggleFieldToggle(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Multi-line text input area.
    static void TextArea(UIView view, string[] style = null, string value = null, string defaultValue = null, string placeholder = null, bool? disabled = null, int? rows = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onSubmit = null, Func<Context, Task> onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView> content = null, string file = "", int line = 0)
    // Single-line text input field.
    static void TextField(UIView view, string[] style = null, string value = null, string defaultValue = null, string placeholder = null, bool? disabled = null, string type = null, string step = null, string min = null, string max = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onSubmit = null, bool? clearOnSubmit = null, Action<UIView> content = null, string file = "", int line = 0)
  // Event args for interact outside events on overlays (combines pointer and focus).
  sealed class InteractOutsideArgs : IEquatable<InteractOutsideArgs>
    // Event args for interact outside events on overlays (combines pointer and focus).
    ctor(string TargetId)
    string TargetId { get; init; }
  // String constants for common keyboard key names, matching the browser KeyboardEvent.key specification. Use these with for type-safe key filtering. Raw strings can also be used for uncommon keys not listed here.
  static class Key
    static string Alt
    static string ArrowDown
    static string ArrowLeft
    static string ArrowRight
    static string ArrowUp
    static string Backspace
    static string Control
    static string Delete
    static string End
    static string Enter
    static string Escape
    static string F1
    static string F10
    static string F11
    static string F12
    static string F2
    static string F3
    static string F4
    static string F5
    static string F6
    static string F7
    static string F8
    static string F9
    static string Home
    static string Meta
    static string PageDown
    static string PageUp
    static string Shift
    static string Space
    static string Tab
  // Event args for keyboard events, matching the browser KeyboardEvent properties.
  sealed class KeyboardEventArgs : IEquatable<KeyboardEventArgs>
    // Event args for keyboard events, matching the browser KeyboardEvent properties.
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
    static void KeyboardListener(UIView view, Func<KeyboardEventArgs, Task> onKeyDown = null, Func<KeyboardEventArgs, Task> onKeyUp = null, IReadOnlyList<string> keys = null, bool? global = true, bool? preventDefault = null, bool? stopPropagation = null, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // Extension methods for scroll area and layout components.
  static class LayoutExtensions
    // Maintains a specific aspect ratio for content.
    static void AspectRatio(UIView view, string[] style = null, double ratio = 1, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Provides text direction context (ltr/rtl) to descendants.
    static void DirectionProvider(UIView view, string[] style = null, Dir dir = Ltr, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Infinite scroll view that fires callbacks when user scrolls near the end.
    static void InfiniteScrollView(UIView view, string[] style = null, int threshold = 200, int debounceMs = 100, bool loading = false, bool hasMore = true, ScrollDirection direction = Down, ScrollAreaScrollbars scrollbars = Vertical, Action<UIView> loadingIndicator = null, Func<ScrollNearEndArgs, Task> onNearEnd = null, Action<UIView> content = null, string[] viewportStyle = null, string[] scrollbarStyle = null, string[] thumbStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Progress component that auto-renders the indicator with transform.
    static void Progress(UIView view, string[] style = null, double? value = null, double? max = null, string variant = null, bool indeterminate = false, Func<double?, string> getValueLabel = null, string[] rootStyle = null, string[] indicatorStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Resizable split panel with a drag handle between two panes. Resize is handled entirely on the client — only the final size is sent to the server via .
    static void ResizableSplit(UIView view, Orientation orientation = Horizontal, double initialSize = 200, double minSize = 100, double maxSize = 500, bool reversed = false, Func<double, Task> onResized = null, Action<UIView> first = null, Action<UIView> second = null, string[] style = null, string[] handleStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // ScrollArea component that auto-renders viewport and scrollbars.
    static void ScrollArea(UIView view, string[] style = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType type = Hover, int? scrollHideDelay = null, Dir dir = Ltr, bool autoScroll = false, string autoScrollKey = null, Action<UIView> content = null, string[] viewportStyle = null, string[] scrollbarStyle = null, string[] thumbStyle = null, string[] cornerStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Visual separator between content.
    static void Separator(UIView view, string[] style = null, Orientation orientation = Horizontal, bool decorative = true, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
  // Event returned from a geolocation action with latitude/longitude coordinates.
  sealed class LocationActionEvent : ActionEvent, IEquatable<LocationActionEvent>
    // Event returned from a geolocation action with latitude/longitude coordinates.
    ctor(bool Success, double? Latitude, double? Longitude, double? Accuracy)
    double? Accuracy { get; init; }
    double? Latitude { get; init; }
    double? Longitude { get; init; }
  // Specifies the behavior of a CaptureButton when pressed.
  enum MediaCaptureButtonMode
    Hold
    Toggle
  // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind.
  sealed class MediaCaptureEvent : IEquatable<MediaCaptureEvent>
    // Event data for media capture start/stop callbacks, containing the stream identifier and capture kind.
    ctor(string StreamId, string Kind)
    string Kind { get; init; }
    string StreamId { get; init; }
  // Specifies the type of media to capture with a CaptureButton.
  enum MediaCaptureKind
    Audio
    Camera
    Screen
  // Extension methods for media playback components.
  static class MediaExtensions
    // Audio player for URL-based audio content.
    static void AudioUrlPlayer(UIView view, string[] style = null, string url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, string preload = null, string className = null, string styleId = null, string key = null, string file = "", int line = 0)
    // Button that captures media (audio, camera, or screen) based on the specified kind. Supports both text mode and icon mode. In text mode (content is null), label is displayed as visible text. In icon mode (content is provided), label becomes the accessible aria-label and content is displayed.
    static void CaptureButton(UIView view, string[] style = null, MediaCaptureKind kind = Audio, string label = null, MediaCaptureButtonMode captureMode = Hold, ClientAudioCaptureOptions audioOptions = null, ClientVideoCaptureOptions videoOptions = null, int? holdReleaseDelayMs = null, bool? disabled = null, string className = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<MediaCaptureEvent, Task> onCaptureStart = null, Func<MediaCaptureEvent, Task> onCaptureStop = null, Action<UIView> content = null, string file = "", int line = 0)
    // Canvas element for rendering a live video stream.
    static void VideoStreamCanvas(UIView view, string[] style = null, string streamId = null, int? width = null, int? height = null, string className = null, string styleId = null, string key = null, string file = "", int line = 0)
    // Video player for URL-based video content.
    static void VideoUrlPlayer(UIView view, string[] style = null, string url = null, bool? controls = null, bool? autoplay = null, bool? loop = null, bool? muted = null, bool? playsInline = null, string poster = null, int? width = null, int? height = null, string className = null, string styleId = null, string key = null, string file = "", int line = 0)
  // Extension methods for NavigationMenu, Menubar, and Toolbar components.
  static class NavigationExtensions
    // Menubar root container.
    static void Menubar(UIView view, string[] style = null, string value = null, string defaultValue = null, Dir dir = Ltr, bool loop = true, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Checkbox item in menu.
    static void MenubarCheckboxItem(UIView view, string[] style = null, CheckedState isChecked = Unchecked, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<CheckedState, Task> onCheckedChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Dropdown content for the menu.
    static void MenubarContent(UIView view, string[] style = null, bool loop = true, Side side = Bottom, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Clickable menu item.
    static void MenubarItem(UIView view, string[] style = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onSelect = null, Action<UIView> content = null, string file = "", int line = 0)
    // Visual indicator for checkbox/radio state.
    static void MenubarItemIndicator(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Individual menu in the menubar.
    static void MenubarMenu(UIView view, string[] style = null, string value = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Radio group in menu.
    static void MenubarRadioGroup(UIView view, string[] style = null, string value = null, string defaultValue = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Radio item in menu.
    static void MenubarRadioItem(UIView view, string[] style = null, string value = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Separator between menu items.
    static void MenubarSeparator(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Submenu container.
    static void MenubarSub(UIView view, string[] style = null, bool? open = null, bool? defaultOpen = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Content for submenu.
    static void MenubarSubContent(UIView view, string[] style = null, bool loop = true, Side side = Right, Align align = Start, double? sideOffset = null, double? alignOffset = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Trigger for submenu.
    static void MenubarSubTrigger(UIView view, string[] style = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Button that opens a menu.
    static void MenubarTrigger(UIView view, string[] style = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Navigation menu root.
    static void NavigationMenu(UIView view, string[] style = null, string value = null, string defaultValue = null, Orientation orientation = Horizontal, int? delayDuration = null, int? skipDelayDuration = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Content shown when navigation item is active.
    static void NavigationMenuContent(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Visual indicator for active navigation item.
    static void NavigationMenuIndicator(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Individual navigation menu item.
    static void NavigationMenuItem(UIView view, string[] style = null, string value = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Link within navigation menu.
    static void NavigationMenuLink(UIView view, string[] style = null, bool? active = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onSelect = null, Action<UIView> content = null, string file = "", int line = 0)
    // List of navigation menu items.
    static void NavigationMenuList(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Trigger that opens navigation content.
    static void NavigationMenuTrigger(UIView view, string[] style = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Viewport for navigation menu content.
    static void NavigationMenuViewport(UIView view, string[] style = null, bool? forceMount = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toolbar container.
    static void Toolbar(UIView view, string[] style = null, Orientation orientation = Horizontal, Dir dir = Ltr, bool loop = true, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Button in the toolbar.
    static void ToolbarButton(UIView view, string[] style = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<Task> onClick = null, Action<UIView> content = null, string file = "", int line = 0)
    // Link in the toolbar.
    static void ToolbarLink(UIView view, string[] style = null, string href = null, string target = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
    // Separator in the toolbar.
    static void ToolbarSeparator(UIView view, string[] style = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, string file = "", int line = 0)
    // Multi-select toggle group in toolbar.
    static void ToolbarToggleGroupMultiple(UIView view, string[] style = null, IReadOnlyList<string> value = null, IReadOnlyList<string> defaultValue = null, bool? rovingFocus = true, bool loop = true, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<IReadOnlyList<string>, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Single-select toggle group in toolbar.
    static void ToolbarToggleGroupSingle(UIView view, string[] style = null, string value = null, string defaultValue = null, bool? rovingFocus = true, bool loop = true, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Action<UIView> content = null, string file = "", int line = 0)
    // Toggle item in toolbar toggle group.
    static void ToolbarToggleItem(UIView view, string[] style = null, string value = null, bool? disabled = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Action<UIView> content = null, string file = "", int line = 0)
  // Represents the orientation for components like Tabs, Slider, etc.
  enum Orientation
    Horizontal
    Vertical
  // Extension methods for overlay component child elements. For the main overlay components (Dialog, AlertDialog, Popover, Tooltip, HoverCard), use the simplified APIs in CoreExtensions.cs which handle Portal/Overlay management automatically.
  static class OverlayExtensions
    // Alert dialog that requires explicit user acknowledgment. Cannot be dismissed by clicking outside.
    static void AlertDialog(UIView view, bool? open = null, bool? defaultOpen = null, string title = null, string description = null, string cancelLabel = null, string actionLabel = null, Func<Task> onAction = null, Action<UIView> trigger = null, Action<UIView> contentSlot = null, string[] overlayStyle = null, string overlayStyleId = null, string[] contentStyle = null, string contentStyleId = null, string[] titleStyle = null, string[] descriptionStyle = null, string[] footerStyle = null, string[] cancelStyle = null, string[] actionStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
    // Modal dialog window.
    static void Dialog(UIView view, string[] style = null, bool? open = null, bool? defaultOpen = null, bool? modal = null, Action<UIView> trigger = null, Action<UIView> contentSlot = null, Action<UIView> content = null, string[] overlayStyle = null, string overlayStyleId = null, string[] contentStyle = null, string contentStyleId = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
    // Rich content card that appears on hover with configurable delays.
    static void HoverCard(UIView view, bool? open = null, bool? defaultOpen = null, int? openDelay = null, int? closeDelay = null, Action<UIView> trigger = null, Action<UIView> contentSlot = null, Action<UIView> content = null, string[] contentStyle = null, string contentStyleId = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
    // Floating content panel that appears next to a trigger element.
    static void Popover(UIView view, bool? open = null, bool? defaultOpen = null, bool? modal = null, Side side = Bottom, Align align = Center, double? sideOffset = null, double? alignOffset = null, Action<UIView> trigger = null, Action<UIView> contentSlot = null, Action<UIView> content = null, string[] contentStyle = null, string contentStyleId = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
    // Toast notification with built-in provider and viewport.
    static void Toast(UIView view, ToastType type = Foreground, bool? open = null, bool? defaultOpen = null, int? durationMs = null, bool? forceMount = null, ToastSwipeDirection swipeDirection = Right, int? swipeThreshold = null, string title = null, string description = null, bool? showClose = null, string closeLabel = null, Action<UIView> content = null, string[] toastStyle = null, string[] viewportStyle = null, string[] titleStyle = null, string[] descriptionStyle = null, string[] closeStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, Func<Task> onEscapeKeyDown = null, Func<Task> onPause = null, Func<Task> onResume = null, Func<ToastSwipeArgs, Task> onSwipeStart = null, Func<ToastSwipeArgs, Task> onSwipeMove = null, Func<ToastSwipeArgs, Task> onSwipeEnd = null, Func<ToastSwipeArgs, Task> onSwipeCancel = null, string file = "", int line = 0)
    // Brief informational message that appears on hover. Includes built-in provider.
    static void Tooltip(UIView view, bool? open = null, bool? defaultOpen = null, double? delayDuration = null, double? skipDelayDuration = null, bool? disableHoverableContent = null, Action<UIView> trigger = null, Action<UIView> contentSlot = null, Action<UIView> content = null, string[] contentStyle = null, string contentStyleId = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
  // One page of items plus the controls needed to render prev/next buttons. Returned by .
  sealed class Page<T> : IEquatable<Page<T>>
    // One page of items plus the controls needed to render prev/next buttons. Returned by .
    ctor(IReadOnlyList<T> Items, int Index, int TotalPages, int PageSize, bool CanPrev, bool CanNext, Func<Task> Prev, Func<Task> Next, Func<int, Task> JumpTo, Func<Task> First, Func<Task> Last, IReadOnlyList<T> Source)
    // True if there is a next page.
    bool CanNext { get; init; }
    // True if there is a previous page.
    bool CanPrev { get; init; }
    // Action that jumps to page 0.
    Func<Task> First { get; init; }
    // Zero-based current page index.
    int Index { get; init; }
    // The slice of for the current page.
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
    // Total number of pages (always >= 1, even when is empty).
    int TotalPages { get; init; }
  // Per-client pagination on top of a server-side list. Holds zero rendering opinion — returns a the caller iterates and binds to whatever prev/next UI suits the design.
  static class PaginationExtensions
    static Page<T> Paginate<T>(UIView view, IReadOnlyList<T> items, ClientReactive<int> page, int pageSize = 20)
  // Options for the Contact Picker API action.
  sealed class PickContactsActionOptions : ActionOptions, IEquatable<PickContactsActionOptions>
    ctor()
    // When true, allows selecting multiple contacts.
    bool Multiple { get; init; }
  // Event args for pointer down outside events on overlays.
  sealed class PointerDownOutsideArgs : IEquatable<PointerDownOutsideArgs>
    // Event args for pointer down outside events on overlays.
    ctor(string TargetId)
    string TargetId { get; init; }
  // Extension methods for QR code generation.
  static class QrCodeExtensions
    // QR code image. Generates a QR code server-side and renders it as an image.
    static void QR(UIView view, string[] style = null, string value = null, int size = 256, string key = null, string file = "", int line = 0)
  // Extension methods for the RichTextEditor component.
  static class RichTextEditorExtensions
    // Inline rich-text editor with a configurable toolbar. Values are HTML strings.
    static void RichTextEditor(UIView view, string value = null, string defaultValue = null, string placeholder = null, bool? disabled = null, IReadOnlyList<RichTextTool> tools = null, bool? showToolbar = null, int? minRows = null, int? maxRows = null, string[] style = null, string[] toolbarStyle = null, string[] toolbarButtonStyle = null, string[] contentStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<string, Task> onSubmit = null, string file = "", int line = 0)
  // Formatting action available in the toolbar.
  enum RichTextTool
    Bold
    Italic
    Underline
    Strikethrough
    Heading1
    Heading2
    Heading3
    Paragraph
    BulletList
    NumberedList
    Blockquote
    Code
    Link
    ClearFormatting
    Undo
    Redo
  // Extension methods for conditionally rendering UI based on user roles.
  static class RoleGatedExtensions
    // Renders content only for admin users.
    static void ForAdmin(UIView view, ClientProfiles profiles, Context clientContext, Action<UIView> content)
    // Renders content only for moderator users.
    static void ForModerator(UIView view, ClientProfiles profiles, Context clientContext, Action<UIView> content)
    // Renders content only if the client has the specified role.
    static void ForRole(UIView view, ClientProfiles profiles, Context clientContext, UserRole role, Action<UIView> content)
    // Renders content only if the client has the specified role (by name).
    static void ForRole(UIView view, ClientProfiles profiles, Context clientContext, string role, Action<UIView> content)
    // Renders content only if the client has any of the specified roles.
    static void ForRoles(UIView view, ClientProfiles profiles, Context clientContext, IEnumerable<UserRole> roles, Action<UIView> content)
  // Represents which scrollbars to show in ScrollAreaSimple.
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
  // Extension methods for the ScrollColumn primitive — a header/body/footer dialog pattern where the body scrolls. Wraps a with the correct flex sizing so scrolling engages without ceremony.
  static class ScrollColumnExtensions
    // Renders a flex column with an optional header, a scrollable body, and an optional footer. The header and footer stay pinned; the body scrolls.
    static void ScrollColumn(UIView view, string[] style = null, Action<UIView> header = null, Action<UIView> footer = null, Action<UIView> content = null, ScrollAreaScrollbars scrollbars = Vertical, ScrollAreaType scrollType = Hover, bool autoScroll = false, string autoScrollKey = null, string[] bodyStyle = null, string[] viewportStyle = null, string[] scrollbarStyle = null, string[] thumbStyle = null, string styleId = null, string key = null, string file = "", int line = 0)
  // Direction for infinite scroll loading.
  enum ScrollDirection
    Down
    Up
  // Event args for when user scrolls near the end of content.
  sealed class ScrollNearEndArgs : IEquatable<ScrollNearEndArgs>
    // Event args for when user scrolls near the end of content.
    ctor(double ScrollTop, double ScrollHeight, double ClientHeight, string Direction)
    double ClientHeight { get; init; }
    string Direction { get; init; }
    double ScrollHeight { get; init; }
    double ScrollTop { get; init; }
  // Extension methods for Select components.
  static class SelectExtensions
    // Select dropdown component that auto-renders the full structure with trigger button, dropdown content, and items. Use either options (flat list) or groups (grouped items) - not both.
    static void Select(UIView view, string[] style = null, IReadOnlyList<SelectOption> options = null, IReadOnlyList<SelectOptionGroup> groups = null, string value = null, string defaultValue = null, string placeholder = null, bool? disabled = null, bool? required = null, bool? open = null, string name = null, string[] triggerStyle = null, string[] contentStyle = null, string[] itemStyle = null, string[] itemIndicatorStyle = null, string indicatorIconName = "check", string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
  // Represents a selectable option in a Select component.
  sealed class SelectOption : IEquatable<SelectOption>
    // Represents a selectable option in a Select component.
    ctor(string Value, string Label, bool Disabled = false)
    bool Disabled { get; init; }
    string Label { get; init; }
    string Value { get; init; }
  // Represents a group of selectable options in a Select component.
  sealed class SelectOptionGroup : IEquatable<SelectOptionGroup>
    // Represents a group of selectable options in a Select component.
    ctor(string Label, IReadOnlyList<SelectOption> Options)
    string Label { get; init; }
    IReadOnlyList<SelectOption> Options { get; init; }
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
    static void ShadertoyCanvas(UIView view, string[] style = null, string shaderSource = null, int? fps = null, IReadOnlyDictionary<string, ShaderUniform> uniforms = null, bool? enableMouse = null, int? width = null, int? height = null, string styleId = null, string key = null, string file = "", int line = 0)
  // Options for the Web Share API action.
  sealed class ShareActionOptions : ActionOptions, IEquatable<ShareActionOptions>
    ctor()
    // Text body for the shared content.
    string Text { get; init; }
    // Title for the shared content.
    string Title { get; init; }
    // URL to share.
    string Url { get; init; }
  // Options for showing a browser notification.
  sealed class ShowNotificationActionOptions : ActionOptions, IEquatable<ShowNotificationActionOptions>
    // Notification body text.
    string Body { get; init; }
    // URL of the notification icon image.
    string Icon { get; init; }
    // Notification title text.
    string Title { get; init; }
  // Represents the side for positioning overlays.
  enum Side
    Top
    Right
    Bottom
    Left
  // Represents sort strategy for @dnd-kit SortableContext.
  enum SortStrategy
    VerticalList
    HorizontalList
  // Contains information about a reorder operation in SortableList.
  sealed class SortableReorderArgs : IEquatable<SortableReorderArgs>
    // Contains information about a reorder operation in SortableList.
    ctor(string ActiveId, string OverId, int OldIndex, int NewIndex, IReadOnlyList<string> NewOrder)
    string ActiveId { get; init; }
    int NewIndex { get; init; }
    IReadOnlyList<string> NewOrder { get; init; }
    int OldIndex { get; init; }
    string OverId { get; init; }
  // Represents sticky behavior for Select/DropdownMenu.
  enum Sticky
    Partial
    Always
  // Defines a tab for use with the Tabs component.
  class TabItem : IEquatable<TabItem>
    // Defines a tab for use with the Tabs component.
    ctor(string Value, string Label, Action<UIView> Content, bool Disabled = false)
    // Builder function for rendering the tab's content panel.
    Action<UIView> Content { get; init; }
    // When true, prevents user interaction with this tab.
    bool Disabled { get; init; }
    // Text label displayed on the tab trigger.
    string Label { get; init; }
    // Unique identifier for the tab.
    string Value { get; init; }
  // Extension methods for Tabs components.
  static class TabsExtensions
    // Container for Tabs components. Use the 'tabs' parameter to define tab content.
    static void Tabs(UIView view, string[] style = null, string value = null, string defaultValue = null, Orientation orientation = Horizontal, ActivationMode activationMode = Automatic, IEnumerable<TabItem> tabs = null, string[] listContainerStyle = null, string[] listStyle = null, string[] triggerStyle = null, string[] disabledTriggerStyle = null, string[] contentContainerStyle = null, string[] contentStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, string file = "", int line = 0)
  // Smallest time unit shown by a .
  enum TimeGranularity
    Hour
    Minute
    Second
  // Extension methods for TimePicker components.
  static class TimePickerExtensions
    // Picker for a time of day. Values are ISO-8601 HH:mm or HH:mm:ss strings.
    static void TimePicker(UIView view, string[] style = null, string value = null, string defaultValue = null, HourFormat hourFormat = Hour24, TimeGranularity granularity = Minute, int? minuteStep = null, int? secondStep = null, bool? disabled = null, bool? open = null, bool? defaultOpen = null, Side side = Bottom, Align align = Start, string placeholder = null, string[] triggerStyle = null, string[] contentStyle = null, string[] columnStyle = null, string[] itemStyle = null, string[] itemSelectedStyle = null, string[] rootStyle = null, string styleId = null, string key = null, IReadOnlyDictionary<string, object> props = null, Func<string, Task> onValueChange = null, Func<bool?, Task> onOpenChange = null, string file = "", int line = 0)
  // Event args for toast swipe events.
  sealed class ToastSwipeArgs : IEquatable<ToastSwipeArgs>
    // Event args for toast swipe events.
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
  // Day of the week used as the first column in the calendar grid.
  enum WeekStart
    Sunday
    Monday

namespace Ikon.Parallax.Themes
  // Defines a UI theme providing base CSS and a default icon library.
  interface ITheme
    // Global CSS injected into the client as the theme baseline.
    string Css { get; }
    // The default icon library name (e.g. "lucide") used when no library is specified on an icon component.
    string DefaultIconLibrary { get; }

namespace Ikon.Parallax.Themes.Ikon
  static class Accessibility
    static string RequiredLabel(string baseLabel)
    static string NotScreenReaderOnly
    static string ScreenReaderOnly
    static string SkipLink
  static class Accordion
    static string ChevronIcon
    static string Content
    static string ContentInner
    static string Default
    static string Header
    static string Item
    static string Root
    static string Trigger
  static class Alert
    static string Base
    static string Default
    static string Description
    static string Error
    static string Info
    static string Success
    static string Title
    static string Warning
  static class AlertDialog
    static string Action
    static string Cancel
    static string Content
    static string Default
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  static class Accessibility.Aria
    static string Busy
    static string Checked
    static string CurrentPage
    static string CurrentStep
    static string Disabled
    static string Expanded
    static string Invalid
    static string Required
    static string Selected
  static class AspectRatio
    static string Base
    static string Default
    static string PlaceholderContent
  static class Avatar
    static string Base
    static string Default
    static string Fallback
    static string Image
    static string Root
  static class Badge
    static string Base
    static string Brand
    static string BrandLg
    static string BrandMd
    static string BrandSm
    static string Default
    static string DefaultLg
    static string DefaultMd
    static string DefaultSm
    static string Error
    static string ErrorLg
    static string ErrorMd
    static string ErrorSm
    static string Grey
    static string GreyLg
    static string GreyMd
    static string GreySm
    static string IconLeft
    static string IconRight
    static string Info
    static string InfoLg
    static string InfoMd
    static string InfoSm
    static string OutlineBrand
    static string OutlineBrandLg
    static string OutlineBrandMd
    static string OutlineBrandSm
    static string OutlineError
    static string OutlineErrorLg
    static string OutlineErrorMd
    static string OutlineErrorSm
    static string OutlineGrey
    static string OutlineGreyLg
    static string OutlineGreyMd
    static string OutlineGreySm
    static string OutlineInfo
    static string OutlineInfoLg
    static string OutlineInfoMd
    static string OutlineInfoSm
    static string OutlineSuccess
    static string OutlineSuccessLg
    static string OutlineSuccessMd
    static string OutlineSuccessSm
    static string OutlineWarning
    static string OutlineWarningLg
    static string OutlineWarningMd
    static string OutlineWarningSm
    static string Success
    static string SuccessLg
    static string SuccessMd
    static string SuccessSm
    static string Warning
    static string WarningLg
    static string WarningMd
    static string WarningSm
  static class Tokens.Blur
    static string Lg
    static string Md
    static string Sm
  static class Breadcrumb
    static string Ellipsis
    static string Item
    static string Link
    static string List
    static string Page
    static string Root
    static string Separator
  static class Button
    static string Base
    static string Default
    static string ErrorLg
    static string ErrorMd
    static string ErrorSm
    static string GhostLg
    static string GhostMd
    static string GhostSm
    static string IconLeft
    static string IconRight
    static string InfoLg
    static string InfoMd
    static string InfoSm
    static string LinkLg
    static string LinkMd
    static string LinkSm
    static string NeutralLg
    static string NeutralMd
    static string NeutralSm
    static string OutlineLg
    static string OutlineMd
    static string OutlineSm
    static string PrimaryLg
    static string PrimaryMd
    static string PrimarySm
    static string SolidLg
    static string SolidMd
    static string SolidSm
    static string SuccessLg
    static string SuccessMd
    static string SuccessSm
    static string WarningLg
    static string WarningMd
    static string WarningSm
  static class Calendar
    static string Day
    static string DayDisabled
    static string DayOutside
    static string DaySelected
    static string DayToday
    static string Default
    static string Grid
    static string Header
    static string HeaderTitle
    static string NavButton
    static string Root
    static string Row
    static string Weekday
  static class Card
    static string Base
    static string Content
    static string Default
    static string Elevated
    static string Flat
    static string Footer
    static string Ghost
    static string Glass
    static string GlassSubtle
    static string Header
    static string HeaderRow
    static string Interactive
    static string InteractiveFill
    static string Outline
    static string Selected
    static string Strong
    static string Subtle
  static class OnSurface.Card
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Carousel
    static string Default
    static string Indicator
    static string IndicatorActive
    static string Indicators
    static string NavButton
    static string Next
    static string Previous
    static string Root
    static string Slide
    static string Track
    static string TrackVertical
    static string Viewport
  static class Chart
    static string Container
    static string ContainerLg
    static string ContainerMd
    static string ContainerSm
    static string ContainerXl
    static string Default
  static class Checkbox
    static string Default
    static string Indicator
    static string Root
  static class CodeEditor
    static string Body
    static string Content
    static string Default
    static string Gutter
    static string Header
    static string LanguageBadge
    static string Line
    static string Root
  static class Collapsible
    static string Content
    static string Default
    static string Root
    static string Trigger
    static string TriggerIcon
  static class ColorPicker
    static string AlphaTrack
    static string Content
    static string Default
    static string HexInput
    static string HueThumb
    static string HueTrack
    static string PresetSwatch
    static string PresetsGrid
    static string SaturationArea
    static string Swatch
    static string SwatchLg
    static string SwatchSm
    static string Thumb
    static string Trigger
  static class Layout.Column
    static string Center
    static string Default
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xs
  static class Command
    static string Default
    static string Dialog
    static string Empty
    static string Group
    static string GroupHeading
    static string Input
    static string InputWrapper
    static string Item
    static string List
    static string Root
    static string Separator
    static string Shortcut
  static class Container
    static string Full
    static string Lg
    static string Md
    static string Prose
    static string Screen
    static string Sm
    static string Xl
    static string Xl2
    static string Xl3
    static string Xl4
    static string Xl5
    static string Xl6
    static string Xl7
    static string Xs
  static class ContentGrid
    static string Bordered
    static string Cell
    static string CellMuted
    static string Default
    static string Header
  static class DataTable
    static string Cell
    static string DataCell
    static string Default
    static string EmptyState
    static string Header
    static string HeaderCell
    static string PageNumber
    static string PageNumberActive
    static string Pagination
    static string PaginationButton
    static string ResizeHandle
    static string Row
    static string RowClickable
  static class DatePicker
    static string Content
    static string Default
    static string Trigger
    static string TriggerLg
    static string TriggerSm
  static class OnSurface.Default
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Dialog
    static string CloseButton
    static string Content
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  static class DragDrop
    static string Container
    static string ContainerHorizontal
    static string DropZone
    static string DropZoneActive
    static string Overlay
    static string OverlayContent
  static class DragDrop.Draggable
    static string Base
    static string Dashed
    static string Default
    static string Disabled
    static string Dragging
  static class Drawer
    static string Content
    static string Default
    static string Description
    static string Footer
    static string Handle
    static string Header
    static string Overlay
    static string Title
  static class DropdownMenu
    static string CheckboxItem
    static string Content
    static string Group
    static string Item
    static string Label
    static string RadioItem
    static string Separator
    static string Shortcut
    static string SubContent
    static string SubTrigger
  static class DragDrop.Droppable
    static string Base
    static string Default
    static string Disabled
    static string Info
    static string Success
  static class Tokens.Duration
    static string Fast
    static string Instant
    static string Normal
    static string Slow
    static string Slower
  static class Transition.Ease
    static string In
    static string InOut
    static string Linear
    static string Out
  static class EmptyState
    static string Actions
    static string Description
    static string IconSize
    static string IconSizeSm
    static string IconWrap
    static string IconWrapSm
    static string IllustrationSize
    static string IllustrationWrap
    static string IllustrationWrapSm
    static string Root
    static string RootFull
    static string RootSm
    static string Title
  static class FeedScroller
    static string Default
    static string MuteToggle
    static string Root
    static string Slide
    static string SlideMedia
    static string SlideOverlay
  static class FileUpload
    static string FileItem
    static string FileList
    static string FileName
    static string FileSize
    static string RemoveButton
    static string TypeIcon
  static class Accessibility.Focus
    static string HighContrast
    static string None
    static string Sentinel
    static string Within
  static class Tokens.FocusRing
    static string Default
    static string Strong
    static string Subtle
  static class FormField
    static string ErrorText
    static string HelpText
    static string Label
    static string LabelRequired
    static string ParamRow
    static string Root
    static string SuccessText
    static string WarningText
  static class Layout.Grid
    static string Cols2
    static string Cols3
    static string Cols4
  static class Select.Group
    static string Label
    static string Root
  static class Helper
    static string Join(params string[] parts)
  static class ImageCard.Hover
    static string Dim
    static string Zoom
  static class HoverCard
    static string Content
    static string Default
  static class Icon
    static string Default
    static string Spinner
    static string SpinnerLg
    static string SpinnerSm
  static class FileUpload.Icon
    static string Base
    static string Brand
    static string Disabled
    static string Error
    static string Info
    static string Success
  static class Toggle.Size.Icon
    static string Lg
    static string Md
    static string Sm
  static class ImageCard
    static string Caption
    static string Image
    static string Root
    static string Title
  static class Input
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultSm
    static string Error
    static string ErrorLg
    static string ErrorSm
    static string Ghost
    static string GhostLg
    static string GhostSm
    static string Invalid
    static string InvalidLg
    static string InvalidSm
    static string Success
    static string SuccessLg
    static string SuccessSm
    static string Warning
    static string WarningLg
    static string WarningSm
  static class Interaction
    static string HoverCard
    static string HoverGlow
    static string HoverLift
  static class DragDrop.Item
    static string Base
    static string Dashed
    static string Default
    static string Disabled
    static string Dragging
  static class Label
    static string Base
    static string Default
    static string Error
    static string Optional
    static string Required
  static class Layout
    static string Center
    static string Page
    static string RowWrap
    static string Section
    static string SectionBody
    static string SectionHeader
    static string Stretch
  static class Media
    static string CanvasFill
    static string Default
    static string EmptyState
    static string Fill
    static string ImageEmptyState
    static string Mirror
    static string PlaceholderHint
    static string PlaceholderIcon
    static string PlaceholderText
    static string VideoContainer
  static class Menubar
    static string Content
    static string Default
    static string Item
    static string Root
    static string Separator
    static string Trigger
  static class Accessibility.Motion
    static string Reduce
    static string ReduceFade
    static string Respectful
    static string Safe
  static class NavItem
    static string Active
    static string ActiveAccent
    static string ActiveBrand
    static string ActiveSubtle
    static string Count
    static string Default
    static string Icon
    static string Label
    static string Lg
    static string Md
    static string Sm
    static string Subtle
  static class NavPanel
    static string Base
    static string Border
    static string Divided
    static string Filled
    static string Ghost
  static class NavSection
    static string Divider
    static string Label
    static string Root
  static class NavigationMenu
    static string Content
    static string ContentNarrow
    static string ContentPopover
    static string ContentPopoverSide
    static string ContentWide
    static string Default
    static string Indicator
    static string Link
    static string LinkCompact
    static string List
    static string ListVertical
    static string Root
    static string Trigger
    static string TriggerDisabled
    static string TriggerIcon
    static string TriggerIconRotate180
    static string TriggerIconRotate90
    static string TriggerVertical
    static string Viewport
  static class OnSurface
  static class Tokens.Opacity
    static string GlassLg
    static string GlassMd
    static string GlassSm
    static string O10
    static string O15
    static string O20
    static string O25
    static string O30
    static string O40
    static string O5
    static string O50
  static class Separator.Orientation
    static string Horizontal
    static string Vertical
  static class OtpField
    static string Default
    static string Input
    static string Root
  static class ImageCard.Overlay
    static string Center
    static string Dim
    static string Reveal
  static class Page
    static string Base
    static string Default
    // Backwards-compatible alias for the tri-color decorative overlay. New code should prefer Showcase for clarity.
    static string Gradient
    static string Plain
    static string Showcase
  static class Pagination
    static string Active
    static string Disabled
    static string Ellipsis
    static string Item
    static string List
    static string Next
    static string Previous
    static string Root
  static class Panel
    static string Fill
    static string Side
    static string Sidebar
    static string SidebarNarrow
    static string Wide
  static class Input.Password
    static string Input
    static string Toggle
    static string Wrapper
  static class Popover
    static string Content
    static string Default
  static class OnSurface.Popover
    static string Caption
    static string Muted
    static string Subtle
    static string Text
  static class Progress
    static string ComposeIndicator(string variant = null, bool indeterminate = false, params string[] overrides)
    static string IndicatorTransform(double value)
    static string Base
    static string Default
    static string Indeterminate
    static string Indicator
    static string IndicatorBase
    static string Label
    static string Root
    static string Value
  static class Transition.Property
    static string All
    static string Colors
    static string Opacity
    static string Shadow
    static string Transform
  static class RadioGroup
    static string Default
    static string Indicator
    static string Item
    static string Root
    static string RootHorizontal
  static class Tokens.Radius
    static string Full
    static string Lg
    static string Md
    static string None
    static string Sm
    static string Xl
    static string Xl2
  static class AspectRatio.Ratio
    static string Photo
    static string Portrait
    static string Square
    static string Video
    static string Wide
  static class ResizableSplit
    static string FirstPane
    static string FirstPaneVertical
    static string Handle
    static string HandleVertical
    static string Root
    static string SecondPane
    static string SecondPaneVertical
  static class Responsive
    static string CenterToEnd
    static string CenterToLeft
    static string CenterToSpaceBetween
    static string CenterToStart
    static string ColToRow
    static string ColToRowMd
    static string HiddenDesktop
    static string HiddenMobile
    static string HiddenTablet
    static string LeftToCenter
    static string RowToCol
    static string VisibleMobile
    static string VisibleTablet
  static class RichTextEditor
    static string Content
    static string Default
    static string Root
    static string Toolbar
    static string ToolbarButton
    static string ToolbarSeparator
  static class Layout.Row
    static string Default
    static string InlineCenter
    static string Lg
    static string Md
    static string Sm
    static string SpaceBetween
    static string Xl
    static string Xs
  static class ScrollArea
    static string Bordered
    static string Default
    static string Root
    static string Scrollbar
    static string Thumb
    static string Viewport
  static class Select
    static string Content
    static string Default
    static string Item
    static string ItemIndicator
    static string Label
    static string ScrollButton
    static string Separator
    static string Trigger
    static string TriggerBase
  static class Separator
    static string Base
    static string Horizontal
    static string Vertical
  static class Tokens.Shadow
    static string Lg
    static string Md
    static string None
    static string Sm
    static string Xl
    static string Xl2
  static class Avatar.Shape
    static string Circle
    static string Square
  static class Skeleton.Shape
    static string Circle
    static string Rectangle
    static string Square
  static class Sheet
    static string Base
    static string CloseButton
    static string Default
    static string Description
    static string Footer
    static string Header
    static string Overlay
    static string Title
  static class Sheet.Side
    static string Bottom
    static string Left
    static string Right
    static string Top
  static class Icon.Size
    static string Lg
    static string Md
    static string Sm
    static string Xs
  static class Button.Size
    static string Icon
    static string Lg
    static string Md
    static string Sm
  static class Toggle.Size
    static string Lg
    static string Md
    static string Sm
  static class Select.Size
    static string Lg
    static string Md
    static string Sm
  static class Progress.Size
    static string Lg
    static string Md
    static string Sm
    static string Xs
  static class Avatar.Size
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xl2
    static string Xs
  static class Skeleton.Size
    static string Lg
    static string Md
    static string Sm
    static string Xl
    static string Xs
  static class Skeleton
    static string Avatar
    static string AvatarLg
    static string AvatarSm
    static string Base
    static string Button
    static string Card
    static string Default
    static string Input
    static string Text
    static string TextLg
    static string TextSm
  static class Slider
    static string Default
    static string Range
    static string Root
    static string RootVertical
    static string Thumb
    static string Track
    static string TrackVertical
  static class Drawer.Snap
    static string Full
    static string Half
    static string Quarter
    static string ThreeQuarter
  static class Layout.Split
    static string Detail
    static string DetailLg
    static string Gapped
    static string Main
    static string Root
    static string Sidebar
    static string SidebarLg
    static string SidebarSm
  static class StatCard
    static string Header
    static string IconBox
    static string IconBoxBrand
    static string IconBoxError
    static string IconBoxInfo
    static string IconBoxSuccess
    static string IconBoxWarning
    static string IconSize
    static string Label
    static string Root
    static string Trend
    static string TrendIcon
    static string TrendLabel
    static string TrendValue
    static string Value
    static string ValueRow
  static class State
    static string Checked
    static string Disabled
    static string Empty
    static string Focusable
    static string Indeterminate
    static string Invalid
    static string Loading
    static string Pending
    static string Pressable
    static string Readonly
    static string Selected
    static string Success
    static string Validating
    static string Warning
  static class Switch
    static string Default
    static string Root
    static string Thumb
  static class Tabs
    static string Content
    static string List
    static string ListVertical
    static string Trigger
    static string TriggerDisabled
  static class Text
    static string Body
    static string BodySm
    static string BodyStrong
    static string Caption
    static string Code
    static string Display
    static string DisplaySm
    static string H1
    static string H2
    static string H3
    static string H4
    static string Label
    static string Link
    static string Muted
    static string Numeric
    static string Overline
    static string Small
    static string Tabular
  static class Textarea
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultSm
    static string Invalid
  sealed class Theme : ITheme
    ctor()
    string Css { get; }
    string DefaultIconLibrary { get; }
    // Returns a per-project themed . Use this to commit to a brand mood (palette, fonts, radii) at the top of your app file so every component inherits it. See for the available knobs.
    static ITheme Custom(Action<ThemeBuilder> configure)
  // Fluent builder for per-project theme overrides. Each setter writes one or more CSS variables that override the base Theme's values via the CSS cascade. Set as few or as many as you need — unset variables keep the base Theme's values.
  sealed class ThemeBuilder
    ctor()
    // Secondary accent color — for badges, chart highlights, the second CTA in a hero. Drives bg-accent, text-accent.
    ThemeBuilder Accent(string color)
    // Page background. Drives bg-background.
    ThemeBuilder Background(string color)
    // Primary brand color — drives bg-brand-solid, the primary button background, focus rings, the brand color ramp seed, AND the shorthand --brand / --primary tokens that models trained on shadcn / Radix conventions reach for. Pass any CSS color (hex, hsl, oklch).
    ThemeBuilder Brand(string color)
    // Card / surface background. Drives bg-card, bg-popover.
    ThemeBuilder Card(string color)
    // Author a parallel set of tokens for dark mode. The same builder methods (Brand, Background, Foreground, Card, Muted, Accent) re-target a dark-mode selector block matching the default theme convention ([data-theme="dark"]) plus .dark for Tailwind's darkMode: 'class' opt-in and a @media (prefers-color-scheme: dark) fallback so a system-level dark preference applies even before the app sets the attribute. Example: Theme.Custom(b => b .Brand("#7C3AED").Background("#FAFAFA").Foreground("#0A0A12") .Dark(d => d.Brand("#A78BFA").Background("#0A0A12").Foreground("#FAFAFA")));
    ThemeBuilder Dark(Action<ThemeBuilder> configure)
    // Body font family. Drives font-body, font-sans.
    ThemeBuilder FontBody(string family)
    // Heading font family. Drives font-heading, font-display.
    ThemeBuilder FontHeading(string family)
    // Primary text color. Drives text-primary AND text-foreground (the shadcn/Tailwind idiom name) so either utility honors the override.
    ThemeBuilder Foreground(string color)
    // Muted text color. Drives text-muted-foreground, text-tertiary, text-quaternary.
    ThemeBuilder Muted(string color)
    // Base border radius. All radius scales (rounded-sm, rounded-md, etc.) are derived from this. Use "0" for sharp, "4px" for modest, "12px" for friendly, "24px" for very friendly.
    ThemeBuilder RadiusBase(string value)
  static class TimePicker
    static string Column
    static string ColumnSeparator
    static string Content
    static string Default
    static string Item
    static string ItemSelected
    static string Trigger
  static class Toast
    static string Action
    static string Base
    static string Close
    static string Default
    static string Description
    static string Title
    static string Viewport
    static string ViewportBottomCenter
  static class Toggle
    static string Base
    static string Default
    static string DefaultLg
    static string DefaultMd
    static string DefaultSm
    static string IconDefault
    static string IconDefaultLg
    static string IconDefaultMd
    static string IconDefaultSm
  static class Tokens
  static class Tone
    static string Error
    static string Ghost
    static string Info
    static string Link
    static string Muted
    static string Neutral
    static string Outline
    static string Primary
    static string Solid
    static string Subtle
    static string Success
    static string Warning
  static class Toolbar
    static string Button
    static string Default
    static string IconStyle
    static string Root
    static string Separator
    static string ToggleGroup
    static string ToggleItem
  static class Tooltip
    static string Content
    static string Default
  static class Transition
    static string Fast
    static string None
    static string Normal
    static string Slow
    static string Slower
  static class StatCard.TrendVariant
    static string Negative
    static string Neutral
    static string Positive
  static class Separator.Variant
    static string Default
    static string Strong
    static string Subtle
  static class Alert.Variant
    static string Default
    static string Error
    static string Info
    static string Success
    static string Warning
  static class Toggle.Variant
    static string Default
  static class Progress.Variant
    static string Default
    static string Error
    static string Success
    static string Warning
  static class Tokens.Width
    static string Dialog
    static string DialogLg
    static string DialogMd
    static string DialogSm
    static string DialogXl
    static string Drawer
    static string Popover
    static string Sheet
    static string Toast
  static class ZIndex
    static string Dropdown
    static string Modal
    static string Overlay
    static string Popover
    static string Sticky
    static string Toast
    static string Tooltip
  static class FileUpload.Zone
    static string Active
    static string ActiveRing
    static string Base
    static string Code
    static string Compact
    static string Default
    static string Disabled
    static string Documents
    static string DragOverlay
    static string Images
    static string Wrapper
