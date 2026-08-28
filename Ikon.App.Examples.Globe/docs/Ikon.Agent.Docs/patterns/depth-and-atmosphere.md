<!-- mined-from: recurring visual-gate verdicts on generated apps ("lacks depth", "flat boxed-in borders", "compose panel lacks depth") -->
# Depth & Atmosphere — surfaces that don't need borders

The most repeated visual-gate criticism across generated apps: every container is a flat fill with a
hard border, so the page reads as boxed-in wireframe rather than designed product. Depth comes from
LAYERED SIGNALS — a soft shadow, a subtle surface tint step, a brand-tinted glow, a restrained
gradient — used deliberately, with borders demoted to hairlines on interactive edges only.

## When to use

Any card, panel, modal, or hero surface in a UI app. Decide the app's depth recipe ONCE (it belongs
in the plan's DESIGN → SHAPE & DEPTH bullet) and apply it consistently — mixing depth recipes reads
as accidental.

## Snippet

```csharp
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
```

## Notes

- One depth signal per surface: tint OR shadow OR gradient. Shadow + border + fill on the same
  box is the "boxed-in" tell the visual gate flags.
- Adaptive apps: shadows read weaker on dark surfaces — a dark theme usually leans on the TINT
  STEP recipe (1) even when light uses shadows (2). Say so in the DESIGN brief.
- Brand-tinted shadows (3) and gradients (4) are hue commitments — take the hues from the app's
  committed palette, never introduce a new accent here.
- Hover elevation (shadow-sm → shadow-md) is the cheapest perceived-quality win on clickable cards.

## See also

- `status-pill` — theme-safe chips; the other recurring theme-defect class.
- `theme-commitment` — adaptive vs fixed scheme choice that this recipe plugs into.
