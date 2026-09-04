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
    // For checkbox cells this is the checked state as the string "true" or "false".
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
    // Per-slot styling (header, rows, cells, pagination, …) goes through styles; see DataTableStyles for the slots.
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
