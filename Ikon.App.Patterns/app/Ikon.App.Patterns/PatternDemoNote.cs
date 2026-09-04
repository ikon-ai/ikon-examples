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
}
