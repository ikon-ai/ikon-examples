namespace Ikon.App.Patterns.Patterns;

// Pattern: pan-zoom-split-layout — see docs/patterns/pan-zoom-split-layout.md.
// The example region below is the canonical body the doc extracts.
internal sealed class PanZoomSplitLayout : IPatternDemo
{
    public string Slug => "pan-zoom-split-layout";
    public string Title => "Pan-and-zoom canvas beside a resizable panel";
    public string Category => "Layout & navigation";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-pan-zoom-split-layout
    private readonly ClientReactive<double> _scale = new(1);
    private readonly ClientReactive<double> _panelWidth = new(280);
    private readonly ClientReactive<PanZoomTapArgs?> _pin = new(null);

    private void Render(IView view)
    {
        // Resize happens entirely on the CLIENT -- only the final size reaches the server through
        // onResized. So the reactive is where the size is remembered, not where it is tracked
        // during the drag.
        view.ResizableSplit(
            orientation: Orientation.Horizontal,
            initialSize: _panelWidth.Value,
            minSize: 200,
            maxSize: 560,
            onResized: async width => _panelWidth.Value = width,

            first: panel => panel.Column(["p-4 gap-2"], content: col =>
            {
                col.Text([Text.H3], text: "Inspector");
                col.Text([Text.Caption], text: _pin.Value is { } pin ? $"Pin at {pin.X:0}, {pin.Y:0} px" : "Tap the plan to place a pin");
            }),

            second: canvas =>
            {
                // PanZoom CLIPS its content, so it needs a size -- without one it collapses and
                // the canvas appears blank rather than mis-sized.
                canvas.PanZoom(
                    ["h-96 w-full bg-card"],
                    scale: _scale.Value,
                    minScale: 0.25,
                    maxScale: 4,

                    // Pan offset and in-gesture zoom live in the client and never round-trip;
                    // only the scale at the END of a gesture is reported. That is why this is
                    // cheap, and also why it cannot drive per-frame server logic.
                    onScaleChange: async scale => _scale.Value = scale,

                    // A tap that did not pan arrives in the content's own unzoomed pixels, so it
                    // is a position on the 2000px-wide plan whatever the zoom.
                    onTap: async tap => _pin.Value = tap,

                    content: inner => inner.Box(["relative w-[2000px]"], content: plan =>
                    {
                        plan.Image(["w-full"], src: "/floorplan.png", alt: "Floor plan");

                        if (_pin.Value is { } pin)
                        {
                            plan.Box([$"absolute size-4 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary left-[{pin.X:0}px] top-[{pin.Y:0}px]"]);
                        }
                    }));

                canvas.Text(["text-muted-foreground text-xs"], text: $"{_scale.Value:P0}");
            });
    }
    #endregion
}
