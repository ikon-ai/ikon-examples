namespace Ikon.Parallax.Components.Standard
  sealed record VideoTapArgs
    ctor(double X, double Y)
    double X { get; init; }
    double Y { get; init; }
  // Performance model: the server emits one wrapper node per item up to itemCount and runs every per-item content builder eagerly server-side (keep content trees inexpensive); the client mounts only the wrappers inside [start - overscan, end + overscan] and leaves the rest out of the DOM entirely. onNearEnd fires when the window enters the last nearEndThreshold rows — append items to grow the list.
  static class VirtualListExtensions
    // columns: Fixed number of columns; ignored when minItemWidthPx is set.
    // rowHeight: Fixed row height in pixels; ignored when aspectRatio is set.
    // overscan: Extra rows to render before/after the visible window. Default 2.
    // gap: Gap in pixels between rows and between columns. Default 12.
    // minItemWidthPx: When set, column count is computed from container width / minItemWidthPx, overriding columns.
    // maxColumns: Upper bound on auto-computed columns (with minItemWidthPx).
    // aspectRatio: Row height = column width × aspectRatio (1.0 = square, 0.75 = 4:3 landscape, 1.4 = portrait card); overrides rowHeight.
    // resetScrollKey: Opaque token that resets scroll to the top whenever it changes (e.g. on filter/sort changes) without remounting the grid.
    // onNearEnd: Fires when scrolled within nearEndThresholdRows rows of the end.
    // nearEndThresholdRows: Distance from end (in rows) to trigger onNearEnd. Default 2.
    static void VirtualGrid(this UIView view, int itemCount, int columns, double rowHeight, Action<UIView, int> onRenderItem, int overscan = 2, int gap = 12, int? minItemWidthPx = null, int? maxColumns = null, double? aspectRatio = null, string? resetScrollKey = null, Func<int, Task>? onNearEnd = null, int nearEndThresholdRows = 2, string[]? style = null, string[]? itemStyle = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
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
  enum WeekStart
    Sunday
    Monday
