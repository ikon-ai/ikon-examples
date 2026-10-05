namespace Ikon.App.Patterns;

// Shared placeholder card for patterns that carry no standalone UI (backend/logic and service
// patterns): the gallery still lists them, and RenderDemo shows what the pattern is plus where to
// read it, rather than a blank pane.
public static class PatternDemoNote
{
    public static void RenderInfo(IView view, string title, string note)
    {
        view.Column(["gap-2 border border-white/10 rounded-lg p-5 bg-white/5 max-w-2xl"], content: card =>
        {
            card.Text(["text-base font-semibold text-[#EDE7DC]"], title);
            card.Text(["text-sm text-[#A8A29E] leading-relaxed"], note);
        });
    }

    // One line under an example that draws nothing of its own (it wires a callback or mutates
    // state), or draws only empty styled containers: what the reader is looking at, or what the
    // example did.
    public static void RenderCaption(IView view, string note)
    {
        view.Text(["text-sm text-[#A8A29E] leading-relaxed max-w-2xl mt-1"], note);
    }
}
