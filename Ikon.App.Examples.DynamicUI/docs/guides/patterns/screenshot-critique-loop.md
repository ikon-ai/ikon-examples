<!-- mined-from: NeonArcade, ParallaxDesigner -->
# Screenshot Critique Loop — Capture Renders, Send To Vision LLM

A custom node (`preview-capture` / `annotation-overlay`) sits inside the rendered preview and exposes a captureRequest reactive. When the request id ticks, the frontend serializes the rendered DOM to a base64 PNG and fires `onCaptureDone` with the bytes. The C# side stores the image and feeds it as an `ImagePart` to a vision LLM (Gemini Flash, Claude Haiku) along with the design plan, asking for structured per-section scores or critique.

## When to use

Any "AI iterates on something visual" loop: generated games, generated UI, image edits. The model can't see what it built unless you feed it the rendered output. Cheap models (Gemini 2.5 Flash) score multiple frames at once and return JSON.

## Snippet

```csharp
// Capture: hidden node inside the rendered preview region
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

// Trigger: bump the request id to capture
private async Task CritiqueUIAsync()
{
    _captureRequestId.Value++;
    if (_lastScreenshotBase64 == null) return;

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
        }).ResultAsync();

    ApplyCritique(critique);
}
```

## Notes

- Use a `Reactive<int> _captureRequestId` and bump it to trigger; the frontend node only re-fires when the value changes. Don't pass a boolean — false→true→false won't survive coalescing.
- Send multiple frames (start screen, mid-gameplay) in a single critique call when scoring time-evolving artifacts; the LLM compares them to detect frozen renderers.
- Prefer the cheapest vision model that can read your screenshots (Gemini Flash, Haiku) — critique is high-frequency.
- Pair this with `Plan-Then-Code` so the critique can be structured per plan section instead of free-text.

## See also

- `plan-then-code-iteration`
- `web-research`
