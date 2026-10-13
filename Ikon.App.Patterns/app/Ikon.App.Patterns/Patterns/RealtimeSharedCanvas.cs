namespace Ikon.App.Patterns.Patterns;

// Pattern: realtime-shared-canvas — see docs/patterns/realtime-shared-canvas.md. Its server code is a
// bare block the corpus compile test checks, and its canvas component is a TypeScript example in
// platform-typescript/sdk/sdk-doc-examples; the gallery frontend registers no custom node to draw it.
internal sealed class RealtimeSharedCanvas : IPatternDemo
{
    public string Slug => "realtime-shared-canvas";
    public string Title => "Real-time shared canvas";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Custom-node pattern with no standalone demo surface: strokes travel as .tp messages in 40 ms batches, a canvas asks for the history on every connect, and the server sends JPEG snapshots to a vision model. See docs/patterns/realtime-shared-canvas.md.");
}
