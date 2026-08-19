public partial class Validation
{
    private readonly Reactive<int> _virtualListItemCount = new(200);
    private readonly Reactive<int> _virtualGridItemCount = new(200);

    private static readonly string[] VirtualGridCellPalette =
    [
        "bg-red-500", "bg-orange-500", "bg-amber-500", "bg-yellow-500",
        "bg-lime-500", "bg-green-500", "bg-emerald-500", "bg-teal-500",
        "bg-cyan-500", "bg-sky-500", "bg-blue-500", "bg-indigo-500",
        "bg-violet-500", "bg-purple-500", "bg-fuchsia-500", "bg-pink-500"
    ];

    private void RenderVirtualizationSection(UIView view)
    {
        view.Column([Layout.Column.Lg, "p-6"], content: section =>
        {
            section.Text([Text.H3], "DOM-Virtualized Containers");
            section.Text([Text.Body, "text-secondary"],
                "VirtualList and VirtualGrid only mount items inside the visible window plus an overscan buffer. Items beyond the window exist as wrapper nodes but their content children are not rendered.");

            section.Box([Card.Default, "p-4"], content: card =>
            {
                card.Column([Layout.Column.Sm], content: col =>
                {
                    col.Text([Text.BodyStrong], "VirtualList — fixed-height vertical list");
                    col.Text([Text.Caption, "text-tertiary"],
                        $"{_virtualListItemCount.Value} items. Each row 56px. onNearEnd appends 200 more (cap 5000).");

                    col.Box(["h-[400px] w-full rounded-md border border-secondary overflow-hidden"], content: box =>
                    {
                        box.VirtualList(
                            itemCount: _virtualListItemCount.Value,
                            itemHeight: 56,
                            overscan: 4,
                            nearEndThreshold: 10,
                            onNearEnd: async _ =>
                            {
                                if (_virtualListItemCount.Value < 5000)
                                {
                                    _virtualListItemCount.Value = Math.Min(5000, _virtualListItemCount.Value + 200);
                                }
                            },
                            onRenderItem: (rowView, index) =>
                            {
                                rowView.Row(["h-full px-4 items-center justify-between border-b border-secondary"], content: row =>
                                {
                                    row.Text([Text.Body], $"Row {index}");
                                    row.Text([Text.Caption, "text-tertiary font-mono"], $"#{index:D5}");
                                });
                            });
                    });
                });
            });

            section.Box([Card.Default, "p-4"], content: card =>
            {
                card.Column([Layout.Column.Sm], content: col =>
                {
                    col.Text([Text.BodyStrong], "VirtualGrid — responsive columns, square aspect");
                    col.Text([Text.Caption, "text-tertiary"],
                        $"{_virtualGridItemCount.Value} cells. minItemWidthPx=180, maxColumns=8, aspectRatio=1. onNearEnd appends 200 more (cap 5000).");

                    col.Box(["h-[500px] w-full rounded-md border border-secondary overflow-hidden"], content: box =>
                    {
                        box.VirtualGrid(
                            itemCount: _virtualGridItemCount.Value,
                            columns: 4,
                            rowHeight: 200,
                            gap: 12,
                            minItemWidthPx: 180,
                            maxColumns: 8,
                            aspectRatio: 1.0,
                            overscan: 2,
                            nearEndThresholdRows: 3,
                            onNearEnd: async _ =>
                            {
                                if (_virtualGridItemCount.Value < 5000)
                                {
                                    _virtualGridItemCount.Value = Math.Min(5000, _virtualGridItemCount.Value + 200);
                                }
                            },
                            onRenderItem: (cellView, index) =>
                            {
                                var bg = VirtualGridCellPalette[index % VirtualGridCellPalette.Length];
                                cellView.Box([$"h-full w-full rounded-md border border-secondary shadow-sm flex flex-col p-3 {bg}"], content: cell =>
                                {
                                    cell.Row([Layout.Row.SpaceBetween, "items-center w-full flex-wrap"], content: header =>
                                    {
                                        header.Text([Text.BodyStrong, "text-white"], $"Cell {index}");
                                        header.Text([Text.Caption, "text-white/80 font-mono"], $"#{index:D5}");
                                    });
                                    cell.Box(["flex-1"]);
                                    cell.Text([Text.Caption, "text-white/70"], $"row {index / 4}, col {index % 4}");
                                });
                            });
                    });
                });
            });
        });
    }
}
