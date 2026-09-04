namespace Ikon.App.Patterns.Patterns;

// Pattern: screenshot-critique-loop — see docs/patterns/screenshot-critique-loop.md.
// The docsnippet region captures the rendered preview and feeds it to a vision LLM; the stubs outside it
// stand in for the capture payload, the critique shape and the caller's apply/execute hooks.
internal sealed class ScreenshotCritiqueLoop : IPatternDemo
{
    public string Slug => "screenshot-critique-loop";
    public string Title => "Screenshot critique loop";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Captures the rendered preview and feeds it to a vision LLM for a structured, per-section critique that drives the next revision. See the source and docs/patterns/screenshot-critique-loop.md.");

    private sealed record ScreenCaptureArgs(string ImageBase64);
    private sealed record StructuredCritique(string Summary, List<string> SectionScores);

    private const string StructuredCritiqueSystemPrompt = "Score each plan section 0-100%.";

    private readonly Reactive<int> _captureRequestId = new(0);
    private readonly Reactive<string> _currentPlan = new("");
    private readonly Reactive<string> _lastGeneratedCode = new("");
    private string? _lastScreenshotBase64;
    private bool _pendingCritiqueRequest;

    private void ApplyCritique(StructuredCritique critique) => throw new NotImplementedException();
    private void ExecuteCodeSync(string code, UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-screenshot-critique-loop
    // Capture: hidden node inside the rendered preview region
    private void RenderPreview(UIView view)
    {
        view.Box(["flex-1 p-4 relative"], content: uiView =>
        {
            var onCaptureDoneId = uiView.CreateAction<ScreenCaptureArgs>(async args =>
            {
                _lastScreenshotBase64 = args.Value.ImageBase64;

                if (_pendingCritiqueRequest)
                {
                    _pendingCritiqueRequest = false;
                    await CritiqueUIAsync();
                }
            });
            uiView.AddNode("preview-capture", new Dictionary<string, object?>
            {
                ["captureRequestId"] = _captureRequestId.Value,
                ["onCaptureDoneId"] = onCaptureDoneId,
            });
            ExecuteCodeSync(_lastGeneratedCode.Value, uiView);
        });
    }

    // Trigger: bump the request id to capture
    private async Task CritiqueUIAsync()
    {
        _captureRequestId.Value++;

        if (_lastScreenshotBase64 == null)
        {
            return;
        }

        List<IMessagePart> parts = [
            new TextPart("Score each plan section 0-100% on how the implementation matches the plan."),
            new ImagePart(Convert.FromBase64String(_lastScreenshotBase64), "image/png"),
        ];
        var ctx = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, parts));

        var critique = await Emerge.Run<StructuredCritique>(LLMModel.Gemini25Flash, ctx,
            pass =>
            {
                pass.SystemPrompt = StructuredCritiqueSystemPrompt;
                pass.Command = $"Plan:\n{_currentPlan.Value}\n\nEvaluate.";
                pass.MaxOutputTokens = 4000;
                pass.UseJson = true;
            });

        ApplyCritique(critique);
    }
    #endregion
}
