namespace Ikon.App.Patterns.Patterns;

// Pattern: status-pill — see docs/patterns/status-pill.md.
// The record and the four fields stand in for the caller's real row/card data the chips label.
internal sealed class StatusPill : IPatternDemo
{
    public string Slug => "status-pill";
    public string Title => "Status pill";
    public string Category => "Feedback";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Theme-safe status chip recipes — neutral, brand-tinted, alpha-accent and solid-accent — that read correctly on both light and dark surfaces without variants. See the source and docs/patterns/status-pill.md.");

    private sealed record Recipe(string Category);
    private readonly Recipe recipe = null!;
    private readonly int done = 0;
    private readonly int goal = 0;
    private readonly Reactive<string?> _filter = new(null);

    private void Render(IView view)
    {
        #region docsnippet:pattern-status-pill
        /// Theme-safe chip recipes for ADAPTIVE apps — each works on light AND dark without variants.

        // 1. NEUTRAL chip (default for categories/tags) — fully semantic, flips automatically.
        view.Box(["inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold bg-muted text-muted-foreground"],
            content: v => v.Text(["text-xs font-semibold"], text: recipe.Category));

        // 2. BRAND-TINTED chip (selected/featured) — semantic brand tokens, flips automatically.
        view.Box(["inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold bg-brand-selected text-primary"],
            content: v => v.Text(["text-xs font-semibold"], text: "Featured"));

        // 3. ACCENT chip in a specific hue (success/warn/info) — ALPHA fill over the theme surface +
        //    a `theme-dark:` text step. The /15 fill tints whatever surface is beneath it, so it reads
        //    correctly on both white cards and dark cards; the text steps down for dark contrast.
        view.Box(["inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold bg-emerald-500/15 text-emerald-700 theme-dark:text-emerald-300"],
            content: v => v.Text(["text-xs font-semibold"], text: $"✓ {done}/{goal}"));

        // 4. SOLID accent chip (strong emphasis, e.g. the active filter) — a 500-step fill with explicit
        //    contrast text is theme-invariant BY DESIGN and safe in both themes (unlike a -100 pastel).
        view.Button(["rounded-full px-4 py-1.5 text-sm font-semibold bg-amber-500 text-white shadow-sm border-0"],
            text: "All", onClick: async () => { _filter.Value = null; });
        #endregion
    }
}
