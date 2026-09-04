namespace Ikon.App.Patterns.Patterns;

// Pattern: pan-zoom-split-layout — see docs/patterns/pan-zoom-split-layout.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class PanZoomSplitLayout : IPatternDemo
{
    public string Slug => "pan-zoom-split-layout";
    public string Title => "Pan-and-zoom canvas beside a resizable panel";
    public string Category => "Layout & navigation";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-pan-zoom-split-layout
    private readonly ClientReactive<double> _scale = new(1);
    private readonly ClientReactive<double> _panelWidth = new(280);

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
                col.Text([Text.H3], text: "Inspector")),

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

                    content: inner => inner.Image(["w-[2000px]"], src: "/floorplan.png", alt: "Floor plan"));

                canvas.Text(["text-muted-foreground text-xs"], text: $"{_scale.Value:P0}");
            });
    }
    #endregion
}
