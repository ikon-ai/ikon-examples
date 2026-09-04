namespace Ikon.App.Patterns.Patterns;

// Pattern: annotation-draw-overlay — see docs/patterns/annotation-draw-overlay.md.
// The stubs outside the region stand in for the preview renderer, the viewport state and the send
// path the app owns; the docsnippet region is the canonical body the doc extracts.
internal sealed class AnnotationDrawOverlay : IPatternDemo
{
    public string Slug => "annotation-draw-overlay";
    public string Title => "Annotation draw overlay";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Overlays a draw-to-annotate node above a live preview and attaches the captured marks as an image to the next AI turn. See the source and docs/patterns/annotation-draw-overlay.md.");

    private sealed record AnnotationCaptureArgs(string ImageBase64);

    private enum ViewportMode { Desktop, Tablet, Mobile }

    private readonly Reactive<ViewportMode> _viewportMode = new(ViewportMode.Desktop);
    private readonly Reactive<string> _lastGeneratedCode = new("");
    private readonly List<IMessagePart> parts = new();

    private void ExecuteCodeSync(string code, UIView view) => throw new NotImplementedException();

    private Task SendAnnotationAsync() => throw new NotImplementedException();

    #region docsnippet:pattern-annotation-draw-overlay
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
    #endregion
}
