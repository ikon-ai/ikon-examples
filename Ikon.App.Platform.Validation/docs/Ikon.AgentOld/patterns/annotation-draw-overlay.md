<!-- mined-from: ParallaxDesigner -->
# Annotation Draw Overlay — User Marks Areas On A Preview

A draw-toggle button enables a transparent canvas overlay sitting above the preview. The user draws orange marker strokes on the rendered output to highlight an area, the canvas serializes itself to base64 PNG when "send" is hit, and the bytes attach to the next LLM request as a second image alongside the screenshot. The model sees both the unmodified preview and the user's marks and applies the change exactly where they pointed.

## When to use

Any time text describes a UI region poorly ("make THIS button blue", "adjust the spacing HERE"). Drawing is faster and clearer than naming. Especially powerful for generative-UI editors and image-edit apps.

## Snippet

```csharp
// State
private readonly Reactive<bool> _annotationMode = new(false);
private string? _annotationImageBase64;

// Render: toggle button
view.Button([_annotationMode.Value ? "bg-orange-500" : "bg-slate-700"],
    label: _annotationMode.Value ? "Drawing... (click to send)" : "Draw on preview",
    onClick: async () =>
    {
        var wasAnnotating = _annotationMode.Value;
        _annotationMode.Value = !_annotationMode.Value;
        if (wasAnnotating && _annotationImageBase64 != null)
        {
            await SendAnnotationAsync();
        }
    });

// Render: preview region with annotation node above it
view.Box(["flex-1 relative overflow-hidden"], content: uiView =>
{
    ExecuteCodeSync(_lastGeneratedCode.Value, uiView);

    var onAnnotationCaptureId = uiView.CreateAction<AnnotationCaptureArgs>(async args =>
    {
        _annotationImageBase64 = args.Value.ImageBase64;
    });
    uiView.AddNode("annotation-overlay", new Dictionary<string, object?>
    {
        ["enabled"] = _annotationMode.Value,
        ["captureRequested"] = _annotationMode.Value,
        ["onAnnotationCaptureId"] = onAnnotationCaptureId,
        ["viewportMode"] = _viewportMode.Value.ToString(),
    });
});

// LLM call attaches both screenshot and annotation
if (_annotationImageBase64 != null)
{
    parts.Add(new TextPart(
        "The user drew orange annotations on the preview to highlight EXACTLY which area they want modified. " +
        "Match them precisely to the UI elements visible in the screenshot above:"));
    parts.Add(new ImagePart(Convert.FromBase64String(_annotationImageBase64), "image/png"));
    _annotationImageBase64 = null;
}
```

## Notes

- Use a high-saturation color like orange (#FF8800) — distinct from typical app palettes, easy for the LLM to localize.
- The annotation overlay is `position: absolute; pointer-events: none` *unless* `enabled` — flip pointer-events on the same prop so the user can draw without blocking clicks during normal use.
- Send screenshot AND annotation as two `ImagePart`s (not a single composite) — gives the LLM the unannotated baseline to compare against.
- Clear `_annotationImageBase64` after sending so a stale doodle doesn't leak into the next turn.
- Phrase the prompt as "the user drew on top of the preview to point at" — without this, the LLM may treat the marks as part of the design.

## See also

- `screenshot-critique-loop`
- `image-gallery`
