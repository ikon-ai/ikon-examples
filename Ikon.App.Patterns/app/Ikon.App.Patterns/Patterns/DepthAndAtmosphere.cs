namespace Ikon.App.Patterns.Patterns;

// Pattern: depth-and-atmosphere — see docs/patterns/depth-and-atmosphere.md.
// The stub bodies stand in for the app's real card/hero/input content builders so the four depth
// recipes the doc extracts render against something concrete.
internal sealed class DepthAndAtmosphere : IPatternDemo
{
    public string Slug => "depth-and-atmosphere";
    public string Title => "Depth and atmosphere";
    public string Category => "Layout";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Four surface-depth recipes — tint step, soft shadow, brand-tinted shadow, and accent gradient — for signalling elevation without heavy borders. See the source and docs/patterns/depth-and-atmosphere.md.");

    private void CardBody(UIView view) => throw new NotImplementedException();
    private void HeroBody(UIView view) => throw new NotImplementedException();
    private void InputRow(UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-depth-and-atmosphere
    private void Render(IView view)
    {
        // Four depth recipes, weakest to strongest. Pick ONE as the app's default card treatment.

        // 1. TINT STEP (quietest — dense/professional UIs): the surface is one shade off the page,
        //    no border, no shadow. Depth reads from the value difference alone.
        view.Box(["rounded-xl bg-card p-5"], content: CardBody);

        // 2. SOFT SHADOW (the default for most apps): shadow does the lifting, border-0. Step the
        //    shadow with elevation — resting sm, hover md — and NEVER pair a heavy shadow with a
        //    heavy border (pick one signal).
        view.Box(["rounded-xl bg-card p-5 shadow-sm hover:shadow-md transition-shadow border-0"],
            content: CardBody);

        // 3. BRAND-TINTED SHADOW (feature cards, CTAs — makes elevation feel branded, not generic):
        //    an arbitrary shadow color derived from the accent. Use on the 1-2 surfaces that deserve
        //    emphasis, not everywhere.
        view.Box(["rounded-2xl bg-card p-6 shadow-lg shadow-cyan-500/20 border-0"], content: HeroBody);

        // 4. ACCENT GRADIENT SURFACE (vivid/fixed-theme apps — heroes, headers, empty states):
        //    a two-stop gradient in the committed palette via arbitrary values. Text on it uses an
        //    explicit contrast color. Gratuitous gradients on every card are the anti-pattern; ONE
        //    atmospheric surface per view is the recipe.
        view.Box(["rounded-2xl p-8 bg-gradient-to-br from-[#0ea5e9] to-[#6366f1] text-white shadow-lg"],
            content: v => v.Text(["text-lg font-semibold text-white"], text: "Weekly summary"));

        // Interactive edges: where a boundary is genuinely needed (inputs, table rows, list dividers),
        // use a HAIRLINE in the theme border token — not a visible frame around whole cards.
        view.Box(["rounded-lg border border-border bg-background px-3 py-2"], content: InputRow);
    }
    #endregion
}
