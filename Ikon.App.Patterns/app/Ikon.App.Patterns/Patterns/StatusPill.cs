namespace Ikon.App.Patterns.Patterns;

// Pattern: status-pill — see docs/patterns/status-pill.md.
// The record and the four fields stand in for the caller's real row/card data the chips label. The
// demo draws the chips on the theme's own card surface, so switching the app between light and dark
// shows the claim the pattern makes.
internal sealed class StatusPill : IPatternDemo
{
    public string Slug => "status-pill";
    public string Title => "Status pill";
    public string Category => "Feedback";

    private sealed record Recipe(string Category);
    private readonly Recipe recipe = new("Weeknight dinners");
    private readonly int done = 3;
    private readonly int goal = 5;
    private readonly Reactive<string?> _filter = new("vegetarian");

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Row(["flex-wrap items-center gap-2 rounded-xl p-4 bg-card border border-secondary"], content: row => Render(row));
            col.Text(["text-xs text-zinc-400"], _filter.Value is { } filter ? $"Active filter: {filter} (press All to clear)" : "Active filter: none");
        });
    }

    private void Render(IView view)
    {
        #region example:pattern-status-pill
        /// Theme-safe chip recipes for ADAPTIVE apps — each works on light AND dark without variants.

        // 1. NEUTRAL chip (default for categories/tags) — fully semantic, flips automatically. `bg-tertiary`, not
        //    `bg-muted`: the baseline's dark `bg-muted` is the same shade as `bg-card`, so the chip would vanish on a card.
        view.Box(["inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold bg-tertiary text-secondary"],
            content: v => v.Text(["text-xs font-semibold"], text: recipe.Category));

        // 2. BRAND-TINTED chip (selected/featured) — semantic brand fill and text tokens, both flip automatically.
        view.Box(["inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold bg-brand-selected text-brand-secondary"],
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
