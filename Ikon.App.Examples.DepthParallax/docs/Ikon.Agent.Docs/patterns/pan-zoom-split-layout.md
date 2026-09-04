<!-- mined-from: Ikon.App.Patterns -->
# Pan-Zoom Canvas Beside A Resizable Panel — What Stays On The Client

Two components that look like they need constant server round-trips and deliberately do not.
`ResizableSplit` resizes entirely on the client and reports only the **final** size;
`PanZoom` keeps the pan offset and the in-gesture zoom on the client and reports only the scale at
the **end** of a gesture.

That is what makes both cheap — and also what bounds them. Neither can drive per-frame server
logic, because the server never sees the frames.

## When to use

An editor or viewer layout: a floor plan, a diagram, a large image, a map beside an inspector. When
the surface needs hit-testing or selection **in the zoomed space**, build a custom node instead —
`PanZoom` transforms its content without telling the server where anything ended up.

## Notes

- **`PanZoom` clips its content, so give it a size** (`h-96`, `w-full`). Without one it collapses
  and the canvas reads as blank rather than as mis-sized, which sends people looking at the content
  instead of the container.
- Scrolling pans; Ctrl/⌘+scroll or a pinch zooms **about the pointer**; dragging pans. None of that
  is configurable, and none of it round-trips.
- `onScaleChange` fires **once per completed gesture**, with the scale already clamped to
  `minScale`/`maxScale` (defaults 0.25 and 4). It is not a stream.
- Pass `scale:` **with** `onScaleChange` for controlled mode, or `defaultScale:` alone for
  uncontrolled. Passing `scale:` without the handler freezes the zoom.
- **`ResizableSplit.onResized` fires at the end of the drag**, so the reactive is where the size is
  remembered, not where it is tracked. Give it `initialSize` from that reactive so the layout
  survives a re-render.
- `reversed: true` anchors the pane to the other edge, which is what a right-hand inspector wants.

## Snippet

```csharp
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
```

## See also

- `custom-react-node-embed` — when the surface needs real hit-testing in the zoomed space.
- `annotation-draw-overlay` — drawing on top of an image.
