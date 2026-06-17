# Custom SVG visual (ring / gauge / arc / dial / radial) built server-side

For a circular progress **ring**, a **gauge/dial**, a **donut**, a radial/**orbital** layout, an arc, or any bespoke vector visual the first-class charts don't cover, build the SVG as a **C# string** and render it with `view.Image(data: bytes, mimeType: "image/svg+xml")`. Zero frontend code, can't go blank.

> For ordinary pie / bar / line charts use the first-class components instead (`view.PieChart`, `view.BarChart`, `view.LineChart` — see the **charts** guide). This pattern is for shapes those don't cover (rings, gauges, arcs, dials, radial/orbital).
>
> Do NOT reach for `view.AddNode` / a custom React canvas for a static visual — that renders blank unless a resolver is registered. A server-side SVG always renders.

## The one rule that prevents the syntax spiral

**Quote every SVG attribute with SINGLE quotes (`'`), and build with a `StringBuilder`.** SVG accepts single-quoted attributes, so your C# string literals never contain a `"` — which means **no `\"` escaping, no raw-string `$$"""` brace gymnastics, and none of the CS1003 / CS1525 / CS8997 cascades** that come from fighting double quotes inside interpolated strings. Format numbers with an explicit format (`:F1`) so culture never injects a comma.

## Snippet — circular progress ring (stroke-dasharray arc)

```csharp
using System.Text;

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
```

Render it (the bytes form — style array first, `data` + `mimeType` named):

```csharp
view.Image(["w-60 h-60"], data: RingSvg(_remaining.Value / (double)_total.Value, "#22d3ee", $"{_remaining.Value}"),
    mimeType: "image/svg+xml", alt: "Progress ring");
```

## Notes

- **Any shape, same recipe:** a gauge is one arc over a half-circle; a donut is N arcs with different `stroke-dasharray`/`stroke-dashoffset`; an orbital/radial layout places items with `cx = ccx + R*Math.Cos(theta)`, `cy = ccy + R*Math.Sin(theta)`. All are just `sb.Append($"...")` lines with single-quoted attributes.
- **Re-render is automatic:** reading a `Reactive`'s `.Value` while building the bytes inside the UI lambda re-renders the image when it changes.
- **No `<text>`-in-SVG culture traps:** keep numbers formatted (`:F1`) and you avoid locale commas in coordinates.
- **Escaping:** because attributes use `'`, the only thing to escape in dynamic text content is `&`, `<`, `>` (rare for numbers/labels). Don't switch to double-quoted attributes — that's what reintroduces the C# escaping spiral.
