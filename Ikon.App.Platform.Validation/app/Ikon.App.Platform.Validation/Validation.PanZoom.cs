public partial class Validation
{
    private static readonly string[] PanZoomTileColors =
    [
        "bg-amber-400", "bg-rose-400", "bg-sky-400", "bg-emerald-400",
        "bg-violet-400", "bg-orange-400", "bg-teal-400", "bg-pink-400",
    ];

    private void RenderPanZoomSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Controlled viewport");
                view.Text([Text.Caption, "mb-4"], "Scroll to pan, Ctrl/⌘+scroll or pinch to zoom about the pointer, drag to pan. The pan offset stays in the client; the scale is reported once per gesture and drives the preset buttons.");

                view.Row(["items-center gap-2 mb-4 flex-wrap"], content: row =>
                {
                    foreach (var preset in new[] { 0.5, 1.0, 2.0 })
                    {
                        var target = preset;
                        row.Button([Button.OutlineSm], text: $"{target:P0}", onClick: async () => _panZoomScale.Value = target);
                    }

                    row.Text([Text.Caption, "ml-2 tabular-nums"], $"Scale {_panZoomScale.Value:0.00}×");
                });

                view.PanZoom(
                    ["h-96 w-full rounded-lg border border-secondary bg-secondary"],
                    scale: _panZoomScale.Value,
                    minScale: 0.25,
                    maxScale: 4,
                    onScaleChange: async scale => _panZoomScale.Value = scale,
                    content: canvas =>
                    {
                        canvas.Box(["flex flex-wrap gap-4 p-6 w-[1600px]"], content: sheet =>
                        {
                            for (var i = 0; i < 32; i++)
                            {
                                var color = PanZoomTileColors[i % PanZoomTileColors.Length];
                                sheet.Box(["w-40 h-28 rounded-lg flex items-center justify-center text-zinc-950 font-semibold shadow-sm", color], content: tile =>
                                {
                                    tile.Text(text: $"Tile {i + 1}");
                                });
                            }
                        });
                    });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Uncontrolled, fitted at 50%");
                view.Text([Text.Caption, "mb-4"], "No scale: and no handler — the viewport keeps its zoom entirely client-side and starts from defaultScale.");

                view.PanZoom(
                    ["h-64 w-full rounded-lg border border-secondary bg-card"],
                    defaultScale: 0.5,
                    content: canvas =>
                    {
                        canvas.Column(["p-8 gap-4 w-[1200px]"], content: page =>
                        {
                            page.Text([Text.H1], "A large sheet");
                            page.Text([Text.Body, "max-w-[900px]"], "Fixed-size content larger than the viewport is the typical case: a floor plan, a board of cards, a diagram. The content is laid out at its natural size and the viewport scales it, so text stays crisp at every zoom level.");
                            page.Row(["gap-4"], content: strip =>
                            {
                                for (var i = 0; i < 6; i++)
                                {
                                    strip.Box(["w-44 h-44 rounded-xl border border-secondary bg-secondary flex items-center justify-center"], content: cell =>
                                    {
                                        cell.Icon(name: "map-pin", size: IconSize.Lg);
                                    });
                                }
                            });
                        });
                    });
            });
        });
    }
}
