<!-- mined-from: ParallaxDesigner -->
# Annotation Draw Overlay — User Marks Areas On A Preview

A draw-toggle button enables a transparent canvas overlay sitting above the preview. The user draws orange marker strokes on the rendered output to highlight an area, the canvas serializes itself to base64 PNG when "send" is hit, and the bytes attach to the next LLM request as a second image alongside the screenshot. The model sees both the unmodified preview and the user's marks and applies the change exactly where they pointed.

## When to use

Any time text describes a UI region poorly ("make THIS button blue", "adjust the spacing HERE"). Drawing is faster and clearer than naming. Especially powerful for generative-UI editors and image-edit apps.

## Snippet

```csharp
private readonly Reactive<bool> _annotationMode = new(false);
private string? _annotationImageBase64;

private void Render(IView view)
{
    view.Button([_annotationMode.Value ? "bg-orange-500" : "bg-slate-700"],
        text: _annotationMode.Value ? "Drawing... (click to send)" : "Draw on preview",
        onClick: async () =>
        {
            var wasAnnotating = _annotationMode.Value;
            _annotationMode.Value = !_annotationMode.Value;

            if (wasAnnotating && _annotationImageBase64 != null)
            {
                await SendAnnotationAsync();
            }
        });

    // The annotation node sits above the live preview; when enabled it captures the marks as a PNG.
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
}

private void AttachAnnotationToRequest()
{
    if (_annotationImageBase64 != null)
    {
        parts.Add(new TextPart(
            "The user drew orange annotations on the preview to highlight EXACTLY which area they want modified. " +
            "Match them precisely to the UI elements visible in the screenshot above:"));
        parts.Add(new ImagePart(Convert.FromBase64String(_annotationImageBase64), "image/png"));

        // Clear so a stale doodle doesn't leak into the next turn.
        _annotationImageBase64 = null;
    }
}
```

## Notes

- Use a high-saturation color like orange (#FF8800) — distinct from typical app palettes, easy for the LLM to localize.
- The annotation overlay is `position: absolute; pointer-events: none` *unless* `enabled` — flip pointer-events on the same prop so the user can draw without blocking clicks during normal use.
- Send screenshot AND annotation as two `ImagePart`s (not a single composite) — gives the LLM the unannotated baseline to compare against.
- Clear `_annotationImageBase64` after sending so a stale doodle doesn't leak into the next turn.
- Phrase the prompt as "the user drew on top of the preview to point at" — without this, the LLM may treat the marks as part of the design.
- **Zoom manipulates the work area, and wheel plus pinch are the primary inputs** — they work from the first build, anchored on the pointer. A slider, a percentage, Fit and Reset are secondary and optional; persistent - / + step buttons are not a default, only an answer to an input environment with no gesture, wheel or keyboard path.
- **Pan by dragging empty canvas** (or space-drag / the platform touch gesture). Free x/y movement is never expressed as a permanent directional arrow pad — keyboard nudge and numeric fields are the non-pointer path, and they belong in context rather than as canvas chrome. Move up / Move down stays right for an ordered list, where the action changes sequence rather than position (`board-move-without-drag`).
- **A canvas is not a records surface.** List toolbars, CRM chrome and dashboard filters belong to a canvas only when the app also has records, files or analytics to manage.

## See also

- `screenshot-critique-loop`
