namespace Ikon.App.Patterns.Patterns;

// Pattern: server-side-svg-visual — see docs/patterns/server-side-svg-visual.md.
// The docsnippet region is the reusable progress-ring builder; the depth layers and the render call
// below it show the same single-quoted-attribute recipe applied to a vessel and to `view.Image`.
internal sealed class ServerSideSvgVisual : IPatternDemo
{
    public string Slug => "server-side-svg-visual";
    public string Title => "Server-side SVG visual";
    public string Category => "Media";
    public void RenderDemo(IView view) => Render(view);

    private readonly Reactive<int> _remaining = new(0);
    private readonly Reactive<int> _total = new(1);

    private readonly StringBuilder sb = new();
    private readonly string jarPath = "M 0 0 L 10 10";
    private readonly double leftEdgeX = 20;
    private readonly double topY = 10;
    private readonly double midY = 60;
    private readonly double bottomY = 110;
    private readonly double cx = 64;
    private readonly double rx = 40;

    // Depth is four cheap layered signals — gradients, a specular sheen, an edge highlight and a
    // ground shadow. Attributes are single-quoted so the C# string literals never contain a '"'.
    private void BuildDepthLayers()
    {
        // In <defs>: gradients are the core depth tool. Single-quoted like everything else.
        sb.Append("<defs>");
        // (1) Vertical surface gradient — light falls from above.
        sb.Append("<linearGradient id='body' x1='0' y1='0' x2='0' y2='1'>");
        sb.Append("<stop offset='0' stop-color='#5b4636' stop-opacity='0.95'/>");
        sb.Append("<stop offset='1' stop-color='#2b211a' stop-opacity='0.98'/>");
        sb.Append("</linearGradient>");
        // (2) Specular highlight — a soft white radial, the "glassy" signal.
        sb.Append("<radialGradient id='spec' cx='0.35' cy='0.25' r='0.5'>");
        sb.Append("<stop offset='0' stop-color='#ffffff' stop-opacity='0.35'/>");
        sb.Append("<stop offset='1' stop-color='#ffffff' stop-opacity='0'/>");
        sb.Append("</radialGradient>");
        sb.Append("</defs>");

        // Body uses the gradient fill (not a flat color), thin rim stroke at LOW opacity.
        sb.Append($"<path d='{jarPath}' fill='url(#body)' stroke='#c8865a' stroke-opacity='0.5' stroke-width='2'/>");
        // Specular sheen ON TOP of the body, clipped to the same shape.
        sb.Append($"<path d='{jarPath}' fill='url(#spec)'/>");
        // (3) Vertical edge highlight — one thin light line near the left edge sells curvature.
        sb.Append($"<path d='M {leftEdgeX} {topY} Q {leftEdgeX - 4} {midY} {leftEdgeX} {bottomY}' stroke='#ffffff' stroke-opacity='0.25' stroke-width='3' fill='none'/>");
        // (4) Ground shadow — a soft ellipse under the object anchors it to the surface.
        sb.Append($"<ellipse cx='{cx}' cy='{bottomY + 8}' rx='{rx * 0.7:F0}' ry='7' fill='#000000' opacity='0.25'/>");
    }

    #region docsnippet:pattern-server-side-svg-visual
    // remaining/total in [0,1] of the ring is filled. strokeColor is any CSS color.
    private static byte[] RingSvg(double fraction, string strokeColor, string label)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        const double size = 240, cx = 120, cy = 120, r = 100, stroke = 16;
        const double circumference = 2 * Math.PI * r;
        var dash = circumference * fraction;          // filled length
        var gap = circumference - dash;               // remaining length

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {size} {size}' font-family='ui-sans-serif,system-ui'>");
        // track (full faint ring)
        sb.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='none' stroke='#27272a' stroke-width='{stroke}'/>");
        // progress arc — start at 12 o'clock by rotating -90° about the centre; round caps
        sb.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='none' stroke='{strokeColor}' stroke-width='{stroke}' ");
        sb.Append($"stroke-linecap='round' stroke-dasharray='{dash:F1} {gap:F1}' transform='rotate(-90 {cx} {cy})'/>");
        // centre label
        sb.Append($"<text x='{cx}' y='{cy}' text-anchor='middle' dominant-baseline='central' fill='#fafafa' font-size='34' font-weight='600'>{label}</text>");
        sb.Append("</svg>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
    #endregion

    // Render it (the bytes form — style array first, `data` + `mimeType` named).
    private void Render(IView view)
    {
        view.Image(["w-60 h-60"], data: RingSvg(_remaining.Value / (double)_total.Value, "#22d3ee", $"{_remaining.Value}"),
            mimeType: "image/svg+xml", alt: "Progress ring");
    }
}
