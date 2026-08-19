public partial class Validation
{
    private readonly Reactive<ImageEditorTool> _drawingTool = new(ImageEditorTool.Brush);
    private readonly Reactive<string> _drawingBrushColor = new("#ef4444");
    private readonly Reactive<int> _drawingBrushWidth = new(8);
    private readonly Reactive<double> _drawingZoom = new(1.0);
    private readonly Reactive<int> _drawingTriggerSave = new(0);
    private readonly Reactive<int> _drawingTriggerUndo = new(0);
    private readonly Reactive<int> _drawingTriggerRedo = new(0);
    private readonly Reactive<string?> _drawingSavedImage = new(null);
    private readonly Reactive<bool> _drawingCanUndo = new(false);
    private readonly Reactive<bool> _drawingCanRedo = new(false);
    private readonly Reactive<string> _drawingSourceUrl = new("/test-images/landscape.svg");
    private readonly Reactive<bool> _drawingHighResolution = new(true);

    private static readonly (string Url, string Label)[] DrawingSources =
    [
        ("/test-images/landscape.svg", "Landscape"),
        ("/test-images/tech.svg", "Tech"),
        ("/test-images/wide.svg", "Wide aspect"),
        ("/test-images/portrait.svg", "Portrait")
    ];

    private static readonly string[] DrawingPalette =
    [
        "#ef4444", "#f97316", "#eab308", "#22c55e", "#06b6d4", "#3b82f6", "#a855f7", "#ec4899", "#000000", "#ffffff"
    ];

    private static readonly (ImageEditorTool Tool, string Icon, string Label)[] DrawingTools =
    [
        (ImageEditorTool.Brush, "brush", "Brush"),
        (ImageEditorTool.Eraser, "eraser", "Eraser"),
        (ImageEditorTool.Text, "type", "Text"),
        (ImageEditorTool.Arrow, "move-up-right", "Arrow"),
        (ImageEditorTool.Region, "square-dashed", "Region")
    ];

    private static readonly int[] DrawingBrushSizes = [2, 4, 8, 16, 32, 48, 64];

    private void RenderDrawingSection(UIView view)
    {
        view.Column([Layout.Column.Lg, "p-6"], content: section =>
        {
            section.Text([Text.H3], "Image Editor Canvas");
            section.Text([Text.Body, "text-secondary"],
                "ImageEditorCanvas exposes brush, eraser, text, arrow, and region tools with zoom, undo/redo, and save triggers.");

            section.Box([Card.Default, "p-3"], content: toolbar =>
            {
                toolbar.Column(["gap-2"], content: rows =>
                {
                    // Row 1: tools
                    rows.Row([Layout.Row.Xs, "flex flex-wrap items-center gap-1"], content: toolsRow =>
                    {
                        foreach (var (toolId, icon, label) in DrawingTools)
                        {
                            var captured = toolId;
                            var active = _drawingTool.Value == toolId;
                            toolsRow.Button([active ? Button.SolidSm : Button.NeutralSm, "gap-1.5"],
                                text: label,
                                icon: icon,
                                onClick: async () => _drawingTool.Value = captured);
                        }
                    });

                    // Row 2: brush size + colors
                    rows.Row(["flex flex-row flex-wrap items-center gap-3"], content: row2 =>
                    {
                        row2.Row([Layout.Row.Xs, "items-center gap-1 flex-wrap"], content: brushRow =>
                        {
                            brushRow.Text([Text.Caption, "text-tertiary whitespace-nowrap mr-1"], "Brush");
                            foreach (var size in DrawingBrushSizes)
                            {
                                var captured = size;
                                var active = _drawingBrushWidth.Value == size;
                                brushRow.Button([active ? Button.SolidSm : Button.NeutralSm],
                                    text: $"{size}",
                                    onClick: async () => _drawingBrushWidth.Value = captured);
                            }
                        });

                        row2.Box(["w-px h-8 bg-secondary"]);

                        row2.Row(["items-center gap-1"], content: paletteRow =>
                        {
                            foreach (var color in DrawingPalette)
                            {
                                var captured = color;
                                var active = _drawingBrushColor.Value == color;
                                paletteRow.Box([$"w-7 h-7 rounded-full border border-secondary cursor-pointer hover:scale-110 transition-transform bg-[{captured}] {(active ? "ring-2 ring-brand ring-offset-2" : "")}"],
                                    onClick: async () => _drawingBrushColor.Value = captured);
                            }
                        });
                    });

                    // Row 3: zoom + history + save
                    rows.Row(["flex flex-row flex-wrap items-center gap-3"], content: row3 =>
                    {
                        row3.Row([Layout.Row.Xs, "items-center gap-1 flex-wrap"], content: zoomRow =>
                        {
                            zoomRow.Button([Button.NeutralSm, Button.Icon], text: "Zoom out",
                                onClick: async () => _drawingZoom.Value = Math.Max(0.5, _drawingZoom.Value - 0.25),
                                content: b => b.Icon([Icon.Default], name: "minus"));
                            zoomRow.Text([Text.Caption, "text-tertiary w-12 text-center"], $"{_drawingZoom.Value:0.0}×");
                            zoomRow.Button([Button.NeutralSm, Button.Icon], text: "Zoom in",
                                onClick: async () => _drawingZoom.Value = Math.Min(4.0, _drawingZoom.Value + 0.25),
                                content: b => b.Icon([Icon.Default], name: "plus"));
                            zoomRow.Button([Button.NeutralSm], text: "1×",
                                onClick: async () => _drawingZoom.Value = 1.0);
                        });

                        row3.Box(["w-px h-8 bg-secondary"]);

                        row3.Row([Layout.Row.Xs, "items-center gap-1 flex-wrap"], content: histRow =>
                        {
                            histRow.Button([Button.NeutralSm, Button.Icon], text: "Undo",
                                disabled: !_drawingCanUndo.Value,
                                onClick: async () => _drawingTriggerUndo.Value = _drawingTriggerUndo.Value + 1,
                                content: b => b.Icon([Icon.Default], name: "undo-2"));
                            histRow.Button([Button.NeutralSm, Button.Icon], text: "Redo",
                                disabled: !_drawingCanRedo.Value,
                                onClick: async () => _drawingTriggerRedo.Value = _drawingTriggerRedo.Value + 1,
                                content: b => b.Icon([Icon.Default], name: "redo-2"));
                        });

                        row3.Box(["w-px h-8 bg-secondary"]);

                        row3.Switch(bind: _drawingHighResolution, label: "High resolution");

                        row3.Box(["w-px h-8 bg-secondary"]);

                        row3.Button([Button.SolidSm, "gap-1.5"], text: "Save", icon: "save",
                            onClick: async () => _drawingTriggerSave.Value = _drawingTriggerSave.Value + 1);
                    });

                    // Row 4: source
                    rows.Row([Layout.Row.Xs, "flex flex-wrap items-center gap-1"], content: srcRow =>
                    {
                        srcRow.Text([Text.Caption, "text-tertiary whitespace-nowrap mr-1"], "Source");
                        foreach (var (url, label) in DrawingSources)
                        {
                            var captured = url;
                            var active = _drawingSourceUrl.Value == url;
                            srcRow.Button([active ? Button.SolidSm : Button.NeutralSm], text: label,
                                onClick: async () => _drawingSourceUrl.Value = captured);
                        }
                    });
                });
            });

            section.Box([Card.Default, "p-2 h-[600px] w-full overflow-hidden"], content: canvasBox =>
            {
                canvasBox.ImageEditorCanvas(
                    style: ["w-full h-full"],
                    src: _drawingSourceUrl.Value,
                    tool: _drawingTool.Value,
                    brushColor: _drawingBrushColor.Value,
                    brushWidth: _drawingBrushWidth.Value,
                    zoom: _drawingZoom.Value,
                    highResolution: _drawingHighResolution.Value,
                    triggerSave: _drawingTriggerSave.Value,
                    triggerUndo: _drawingTriggerUndo.Value,
                    triggerRedo: _drawingTriggerRedo.Value,
                    onSave: async args =>
                    {
                        _drawingSavedImage.Value = args.ImageData;
                    },
                    onHistoryChange: async args =>
                    {
                        _drawingCanUndo.Value = args.CanUndo;
                        _drawingCanRedo.Value = args.CanRedo;
                    },
                    key: $"drawing-canvas-{_drawingSourceUrl.Value}");
            });

            if (_drawingSavedImage.Value != null)
            {
                section.Box([Card.Default, "p-3"], content: savedCard =>
                {
                    savedCard.Column([Layout.Column.Sm], content: c =>
                    {
                        c.Row([Layout.Row.SpaceBetween, "items-center flex-wrap"], content: header =>
                        {
                            header.Text([Text.Label], "Last saved image");
                            header.Button([Button.GhostSm],
                                onClick: async () => _drawingSavedImage.Value = null,
                                content: b => b.Icon([Icon.Default, "w-3.5 h-3.5"], name: "x"));
                        });
                        c.Image(["max-h-32 w-auto rounded-md border border-secondary"],
                            src: _drawingSavedImage.Value!, alt: "Saved");
                    });
                });
            }
        });
    }
}
