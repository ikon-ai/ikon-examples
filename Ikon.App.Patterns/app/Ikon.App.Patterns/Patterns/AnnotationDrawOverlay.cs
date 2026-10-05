namespace Ikon.App.Patterns.Patterns;

// Pattern: annotation-draw-overlay — see docs/patterns/annotation-draw-overlay.md.
// The stubs outside the region stand in for the preview renderer, the viewport state and the send
// path the app owns; the example region is the canonical body the doc extracts.
internal sealed class AnnotationDrawOverlay : IPatternDemo
{
    public string Slug => "annotation-draw-overlay";
    public string Title => "Annotation draw overlay";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Overlays a draw-to-annotate node above a live preview and attaches the captured marks as an image to the next AI turn. See the source and docs/patterns/annotation-draw-overlay.md.");

    private sealed record AnnotationCaptureArgs(string ImageBase64, int RequestId);

    private enum ViewportMode { Desktop, Tablet, Mobile }

    private readonly Reactive<ViewportMode> _viewportMode = new(ViewportMode.Desktop);
    private readonly Reactive<string> _lastGeneratedCode = new("");
    private readonly List<IMessagePart> parts = new();

    private void ExecuteCodeSync(string code, UIView view) => throw new NotImplementedException();

    private Task SendAnnotationAsync() => throw new NotImplementedException();

    #region example:pattern-annotation-draw-overlay
    private readonly Reactive<bool> _annotationMode = new(false);
    private readonly Reactive<int> _annotationCaptureRequestId = new(0);
    private (int RequestId, TaskCompletionSource<string> Reply)? _annotationCapture;
    private string? _annotationImageBase64;

    private void Render(IView view)
    {
        view.Button([_annotationMode.Value ? "bg-orange-500" : "bg-slate-700"],
            text: _annotationMode.Value ? "Drawing... (click to send)" : "Draw on preview",
            onClick: async () =>
            {
                if (!_annotationMode.Value)
                {
                    _annotationMode.Value = true;
                    return;
                }

                // Capture BEFORE leaving draw mode: turning the overlay off unmounts the canvas and
                // its marks. Each bump of the request id is one capture, and the overlay answers
                // every one, echoing the id, with an empty string when nothing was drawn. A second
                // click before the reply supersedes the first, which then leaves the state alone.
                _annotationCapture?.Reply.TrySetCanceled();
                var requestId = _annotationCaptureRequestId.Value + 1;
                var reply = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _annotationCapture = (requestId, reply);
                _annotationCaptureRequestId.Value = requestId;

                string imageBase64;
                try
                {
                    imageBase64 = await reply.Task.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException)
                {
                    // Superseded: the newer capture owns draw mode and the image.
                    return;
                }
                catch (TimeoutException)
                {
                    // A client without the overlay never answers; leaving draw mode unmarked is all there is to do.
                    imageBase64 = "";
                }
                finally
                {
                    if (_annotationCapture?.RequestId == requestId)
                    {
                        _annotationCapture = null;
                    }
                }

                _annotationMode.Value = false;

                if (imageBase64.Length > 0)
                {
                    _annotationImageBase64 = imageBase64;
                    await SendAnnotationAsync();
                }
            });

        // The annotation node sits above the live preview; each new captureRequestId makes it
        // send its marks back as a PNG.
        view.Box(["flex-1 relative overflow-hidden"], content: uiView =>
        {
            ExecuteCodeSync(_lastGeneratedCode.Value, uiView);

            var onAnnotationCaptureId = uiView.CreateAction<AnnotationCaptureArgs>(async args =>
            {
                // A reply to a superseded request is stale.
                if (_annotationCapture is { } capture && capture.RequestId == args.Value.RequestId)
                {
                    capture.Reply.TrySetResult(args.Value.ImageBase64);
                }
            });
            uiView.AddNode("annotation-overlay", new Dictionary<string, object?>
            {
                ["enabled"] = _annotationMode.Value,
                ["captureRequestId"] = _annotationCaptureRequestId.Value,
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
