namespace Ikon.App.Patterns.Patterns;

// Pattern: realtime-game-loop — see docs/patterns/realtime-game-loop.md. Its server code is a bare
// block the corpus compile test checks, and its canvas component is a TypeScript example in
// platform-typescript/sdk/sdk-doc-examples; the gallery frontend registers no custom node to draw it.
internal sealed class RealtimeGameLoop : IPatternDemo
{
    public string Slug => "realtime-game-loop";
    public string Title => "Real-time game loop";
    public string Category => "Realtime";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Custom-node pattern with no standalone demo surface: the server ticks and sends the game state as a .tp message every tick, and each client's canvas draws its own frames from it. See docs/patterns/realtime-game-loop.md.");
}
