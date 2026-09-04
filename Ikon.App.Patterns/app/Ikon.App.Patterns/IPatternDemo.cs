namespace Ikon.App.Patterns;

// The uniform entry point the gallery and the render smoke-test drive every pattern through.
// A pattern's own illustrative render method stays private and idiomatic (that is what the docs
// extract); RenderDemo is the thin public bridge to it, rendering the pattern in its default state.
public interface IPatternDemo
{
    string Slug { get; }

    string Title { get; }

    string Category { get; }

    void RenderDemo(IView view);
}
