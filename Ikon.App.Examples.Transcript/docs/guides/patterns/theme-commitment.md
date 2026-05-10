# Theme Commitment — `new IkonTheme { ... }` at the App declaration site

When the brief declares a STYLING dimension (mood / palette / typography), the brand commitment lives at the App's UI declaration as a `new IkonTheme { ... }` object initializer — NOT in `IkonTheme.cs`'s Css raw-string. Each entry is one CSS variable, indexer-keyed: `["primary"] = "amber-400"`, `["background"] = "zinc-950"`, `["radius-base"] = "rounded-lg"`. Every component inherits.

## When to use

Any app whose plan has a STYLING section that names a mood, palette, or typography. If the plan says "fintech minimal" or "vintage editorial" or "retro arcade", the right artifact to edit is the App's UI field — paste a `new IkonTheme { ... }` initializer. No `IkonTheme.cs` edit needed.

## Snippet

```csharp
// Pasted at the top of your App class — the Coder's standard styling step:
private UI UI { get; } = new(app, new IkonTheme
{
    // Brand cluster — set every brand-tinted CSS var explicitly.
    ["primary"]              = "amber-400",
    ["bg-brand-solid"]       = "amber-400",
    ["bg-brand-solid-hover"] = "amber-500",
    ["text-brand"]           = "amber-400",
    ["border-brand"]         = "amber-400",
    ["primary-foreground"]   = "#0A0A0A",   // pick contrast yourself

    // Surfaces.
    ["background"]   = "zinc-950",
    ["text-primary"] = "zinc-50",
    ["card"]         = "zinc-900",
    ["popover"]      = "zinc-900",
    ["muted"]        = "zinc-700",

    // Type + shape + motion.
    ["font-heading"]         = "font-sans",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-2xl",
    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-out",
});
```

The Coder gets the role/value pairs from a `style_mood(brief, notes)` call (the Styling Oracle returns 10-15 coherent tokens, expanded into ~20-30 indexer entries by the tool formatter) and pastes them verbatim. Direct authoring works too — the values are Crosswind class names (`amber-400`, `zinc-950`, `rounded-lg`, `font-sans`), so the LLM speaks the same vocabulary it uses in component class arrays.

## How — the Styling Oracle (preferred)

The Coder gets four styling tools wired in:

```
style_mood(brief, notes)              → 10-15 coherent role/value pairs (palette, typography, shape, motion)
                                         → seeds the active mood for follow-up calls
                                         → PROPAGATES (paste into `new IkonTheme { ... }`)
style_token(intent)                   → one mood-coherent role+value (e.g. "danger color")
                                         → PROPAGATES (paste into `new IkonTheme { ... }`)
style_set(role, value, customName?)   → direct passthrough, no LLM call
                                         → PROPAGATES
style_class(intent)                   → Crosswind utility-class fragment for ONE component
                                         → DOES NOT PROPAGATE — paste inline in that component's class array
                                         → web-only (no Flutter analog)
```

**Standard flow** (once early in Coding, parallel with `plan_read` and `guide()`):

```csharp
await style_mood(
    brief: "fintech minimal — single mint accent, soft radii",
    notes: "")  // or any user-named colors / fonts the brief specified
```

The Oracle returns a list of `{ Role, Value, CustomName?, Rationale }` records. Paste named roles as `PascalCase = "value"` property assignments and custom roles as `["name"] = "value"` indexer entries:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    Brand        = "emerald-500",   /* mint — fintech accent */
    Background   = "zinc-50",       /* near-white canvas */
    Foreground   = "zinc-950",      /* high-contrast body */
    Card         = "white",
    Muted        = "zinc-500",
    Border       = "zinc-200",
    FontHeading  = "font-sans",
    FontBody     = "font-sans",
    RadiusBase   = "rounded-md",
});
```

Mid-Coding, when a novel mood-coherent THEME-LEVEL need arises:

```csharp
await style_token(intent: "a danger color used by all destructive buttons");
// → { Role: "custom", Value: "rose-600", CustomName: "danger",
//     Rationale: "muted brick — reads warning without breaking the mint palette" }
```

Add it to the same `new IkonTheme { ... }` initializer as an indexer entry:

```csharp
["danger"] = "rose-600",
```

For ONE-OFF decorations on a single component (rainbow text on a hero, glow on the submit CTA only):

```csharp
await style_class(intent: "rainbow gradient text for the hero title");
// → { ClassFragment: "bg-gradient-to-r from-rose-400 via-amber-300 to-rose-500 bg-clip-text text-transparent",
//     Rationale: "warm cross-palette gradient" }
```

Paste it inline in that one component's class array — does NOT enter the theme:

```csharp
view.Text([Text.H1, "bg-gradient-to-r from-rose-400 via-amber-300 to-rose-500 bg-clip-text text-transparent"], "Welcome");
```

For tweaks the LLM is sure of — `style_set` skips the LLM:

```csharp
await style_set(role: "brand", value: "rose-600");
// → { Role: "brand", Value: "rose-600", Rationale: "direct override (style_set)" }
```

## Theme-level vs. one-off — scope discipline

| Scope | Where it lives | Tool |
|---|---|---|
| **Theme-level** (propagates) | indexer entry inside `new IkonTheme { ... }` | `style_mood`, `style_token`, `style_set` |
| **One-off** (single component) | inline class string in that component's array | `style_class` |

Default to theme-level for: palette colors, font families, base radius, base motion, base shadow. Default to one-off for: gradient text on a hero, a single button's glow, a validation-error shake, a one-component motion stagger.

## Per-token overrides

For Tailwind palette / radius / shadow / font overrides, or any free CSS variable, just add another indexer entry to the same initializer block:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    ["primary"]      = "amber-400",
    ["background"]   = "zinc-950",
    ["text-primary"] = "amber-50",

    // Re-skin a Tailwind palette step — every bg-amber-400 / text-amber-400
    // / border-amber-400 in the app picks this up:
    ["amber-400"]  = "#F5A524",

    // Tune one radius rung — rounded-lg becomes 1.25rem:
    ["rounded-lg"] = "1.25rem",

    // One shadow rung:
    ["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",

    // Bespoke decorative token, referenced inline as bg-[var(--hero-glow)]:
    ["hero-glow"]  = "radial-gradient(circle, #F5A52488, transparent 70%)",
});
```

The renderer dispatches by key shape: Tailwind palette step (`amber-400`) → `--color-amber-400`, `rounded-*` → `--radius-*`, `shadow-*` → `--shadow-*`, `font-*` → `--font-*`, `ease-*` → `--ease-*`. Anything else falls through as `--{key}: {value}` (with smart sniff so Tailwind tokens used as values still resolve).

## Direct edit (escape hatch)

You can still author the values directly — the Oracle is a librarian, not a gatekeeper. Use the direct path when:

- The brief is extremely specific ("primary is exactly `#d92626`").
- A human dev is editing outside the codegen flow.

Just pass the value verbatim — the indexer accepts raw hex / rem / family-stack values as fallback for cases that don't fit Crosswind tokens:

```csharp
new IkonTheme
{
    ["primary"]      = "#d92626",
    ["radius-base"]  = "0.625rem",
    ["font-heading"] = "Quicksand",
}
```

## Notes

- **Pass `new IkonTheme { ... }` to `UI`** when STYLING is in the plan. **Never `Theming.Apply(...)`, `Theming.Custom(...)`, or `Theme.Custom(...)`** — those factories were retired; the configurable surface is the `IkonTheme` class with `DarkMode` plus an indexer for every CSS variable.
- **At minimum, the Oracle should set the brand cluster (`primary`, `bg-brand-solid`, `bg-brand-solid-hover`, `text-brand`, `border-brand`, `primary-foreground`), the page surfaces (`background`, `text-primary`, `card`), plus typography (`font-heading` / `font-body`) and shape (`radius-base`).** With these set, `Button.Default`, `Card.Default`, `Layout.Page` all render in the brand palette automatically.
- **For dark mode**, set `DarkMode = new IkonTheme { ... }` with the dark-theme overrides. The renderer emits `[data-theme="dark"]` / `.dark` / `prefers-color-scheme: dark` selectors.
- **For non-Tailwind palettes**, pass raw hex (`["primary"] = "#ffd54f"`). The resolver passes raw values through unchanged.
- **Pick contrast yourself.** There is no auto-derived `primary-foreground`. Light brand step (≤ 500) → `"#0A0A0A"`. Dark brand step (≥ 600) → `"#ffffff"`.
- **Don't repeat color in component class arrays.** Once `["primary"]` and `["bg-brand-solid"]` are set, `Button.Default` (which references `bg-primary`) picks it up. Hand-rolling `bg-zinc-950` per button breaks the brand commitment AND breaks dark theme.

## See also

- `typical-app-structure` — the skeleton; pair with this pattern when STYLING is declared.
- `crosswind-styling-and-motion-guide` (top-level guide) — full Crosswind utility class reference.
- `ikon-theming-guide` (top-level guide) — high-level roles, indexer overrides, mood cookbook for third-party reach.
