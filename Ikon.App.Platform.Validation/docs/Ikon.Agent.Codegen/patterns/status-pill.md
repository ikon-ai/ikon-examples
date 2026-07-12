<!-- mined-from: live generated-app audits (RecipeBox / HabitTracker dark-mode defects) -->
# Status Pill — theme-safe chips, badges, and tags

Small rounded chips that label state or category: "Dessert", "✓ 3/5", "Active", "Draft". The single
most common theme bug in generated apps lives here: the classic Tailwind chip recipe
(`bg-amber-100 text-amber-800`) is a LIGHT-theme recipe — those literals do not flip with the theme,
so in dark mode the chip renders as a glowing pastel strip on a dark card. In an ADAPTIVE app, build
chips from the recipes below instead. (In a FIXED-theme app, hardcoded chip colors are fine.)

## When to use

Any small labelled state: category tags on cards, status badges in tables/rows, count pills
("3/5"), filter chips. One accent family per meaning — don't rainbow.

## Snippet

```csharp
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
```

## Anti-pattern

```csharp
// WRONG in an adaptive app — light-pastel literals never flip; this glows on dark cards:
view.Box(["rounded-full px-3 py-1 text-xs bg-amber-100 text-amber-800"], ...);
// Same trap: bg-slate-50 dialog footers, bg-*-50 hover fills, border-*-200 outlines on toggles.
// Use `bg-muted`, `bg-brand-selected`, an alpha fill (`bg-amber-500/15` + `theme-dark:` text),
// or a solid 500-step with explicit contrast text.
```
