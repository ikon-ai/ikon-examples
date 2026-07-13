# Theme Commitment — `new IkonTheme { ... }` at the App declaration site

Every app commits a theme at the App's UI declaration as a `new IkonTheme { ... }` object initializer — NOT in `IkonTheme.cs`'s Css raw-string. Each entry is one theme key: `["primary"] = "amber-400"`, `["background"] = "zinc-950"`, `["radius"] = "rounded-lg"`. Every component inherits.

## When to use

**Every app — always.** A committed theme is part of the commercial-grade UI bar (see the styling guide), not an opt-in for "design" briefs. Even a plain "todo app" or "poll" gets a `new IkonTheme { ... }`: the bare default `new IkonTheme()` reads as unfinished. If the plan has a STYLING section naming a mood ("fintech minimal", "vintage editorial", "retro arcade"), honor it; if it doesn't, still pick a cohesive palette that fits the app's domain and commit it. The right artifact is always the App's UI field — paste a `new IkonTheme { ... }` initializer. No `IkonTheme.cs` edit needed.

**Two-tier scope rule:** only the STRUCTURAL core goes in the theme — surfaces, text, borders, the brand line, radius, fonts, density, motion defaults. Decorative/expressive values (gradients, textures, glows, one-off colors) stay CONCRETE at the use point in that component's class array; they carry the app's personality and have no token obligation.

## Snippet

```csharp
// Pasted at the top of your App class — the Coder's standard styling step.
// ONE line commits the whole brand cluster: ["primary"] expands to the CTA and
// solid fills (+ hovers), focus rings, brand borders, brand icons, and brand text tiers.
private UI UI { get; } = new(app, new IkonTheme
{
    ["primary"]            = "amber-400",
    ["primary-foreground"] = "#0A0A0A",   // text on brand fills — only needed for LIGHT brand steps (white default)

    ["background"]       = "zinc-950",
    ["foreground"]       = "zinc-50",
    ["card"]             = "zinc-900",
    ["muted-foreground"] = "zinc-500",
    ["border"]           = "zinc-800",

    ["radius"]           = "rounded-2xl",
    ["density"]          = "comfortable",   // compact | comfortable | airy — whole-app whitespace
    ["font-heading"]     = "Crimson Pro",   // literal family name — Google Fonts import is automatic

    DarkMode = new IkonTheme
    {
        ["primary"]    = "amber-300",
        ["background"] = "zinc-950",
        ["foreground"] = "zinc-50",
        ["card"]       = "zinc-900",
    },
});
```

For a single committed scheme (vivid/expressive briefs — a neon arcade, a pirate tavern — where a light/dark flip makes no sense), pin the palette instead of authoring a dark variant:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    Mode = ThemeMode.Fixed,   // no OS dark flip, no toggle flip — this palette IS the app
    ["primary"]    = "violet-400",
    ["background"] = "zinc-950",
    ["foreground"] = "cyan-300",
    ["card"]       = "zinc-900",
    ["radius"]     = "rounded-none",
    ["density"]    = "compact",
});
```

`Mode = ThemeMode.Fixed` and a `DarkMode` block are mutually exclusive (Fixed + DarkMode throws). Pick the dark story explicitly — the plan's THEME STRATEGY makes this call.

To refine one variable inside the `primary` cluster, add the explicit canonical key AFTER it (later entries win): `["bg-brand-solid-hover"] = "amber-500"` for a distinct hover shade, `["border-brand"] = "#000000"` for a contrasting brand border.

The Coder gets the role/value pairs from a `style_mood(brief, notes)` call (the Styling Oracle returns coherent role/value tokens, expanded into indexer entries by the tool formatter) and pastes them verbatim. Direct authoring works too — the values are Crosswind tokens (`amber-400`, `zinc-950`, `rounded-lg`, `airy`), the same vocabulary the LLM uses in component class arrays.

## How — the Styling Oracle (preferred)

The Coder gets four styling tools wired in:

```
style_mood(brief, notes)              → coherent role/value pairs (palette, typography, shape, density, motion)
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

The Oracle returns a list of `{ Role, Value, CustomName?, Rationale }` records. Paste every pair as an indexer entry:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    ["primary"]          = "emerald-500",  /* mint — fintech accent */
    ["background"]       = "zinc-50",      /* near-white canvas */
    ["foreground"]       = "zinc-950",     /* high-contrast body */
    ["card"]             = "#ffffff",
    ["muted-foreground"] = "zinc-500",
    ["border"]           = "zinc-200",
    ["radius"]           = "rounded-md",
    ["density"]          = "comfortable",
});
```

Mid-Coding, when a novel mood-coherent THEME-LEVEL need arises:

```csharp
await style_token(intent: "a danger color used by all destructive buttons");
// → { Role: "custom", Value: "rose-600", CustomName: "danger",
//     Rationale: "muted brick — reads warning without breaking the mint palette" }
```

Add it to the same `new IkonTheme { ... }` initializer as an indexer entry (note: for destructive chrome specifically, the built-in `["destructive"]` key already fans out to the whole error cluster):

```csharp
["destructive"] = "rose-600",
```

For ONE-OFF decorations on a single component (rainbow text on a hero, glow on the submit CTA only) — the expressive tier:

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
| **Theme-level** (structural core, propagates) | indexer entry inside `new IkonTheme { ... }` | `style_mood`, `style_token`, `style_set` |
| **One-off** (expressive, single component) | inline class string in that component's array | `style_class` |

Default to theme-level for: the brand line, surface/text/border colors, font families, radius, density, motion defaults. Default to one-off for: gradient text on a hero, a single button's glow, textures, a validation-error shake, a one-component motion stagger — anything decorative. Expressive values at use points are ENCOURAGED; coherence is judged against the plan's DESIGN brief, not by token count.

## Per-token overrides

For Tailwind palette / radius / shadow / font overrides, or a deliberate custom variable, add another indexer entry to the same initializer block:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    ["primary"]    = "amber-400",
    ["background"] = "zinc-950",
    ["foreground"] = "amber-50",

    // Re-skin a Tailwind palette step — every bg-amber-400 / text-amber-400
    // / border-amber-400 in the app picks this up:
    ["amber-400"]  = "#F5A524",

    // Tune one radius rung — rounded-lg becomes 1.25rem:
    ["rounded-lg"] = "1.25rem",

    // One shadow rung:
    ["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",

    // Deliberate custom variable (the -- prefix marks it intentional),
    // referenced inline as bg-[var(--hero-glow)]:
    ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)",
});
```

The renderer dispatches by key shape: theme keys expand to their canonical variable cluster; Tailwind palette steps (`amber-400`) → `--color-amber-400` (Ikon-scale families like `neutral-900` also move the semantic ramp); `rounded-*` → `--radius-*`; `shadow-*` → `--shadow-*`; `font-*` → `--font-*`. Unknown keys emit a dead variable and log a one-time warning — prefix with `--` to declare a custom variable on purpose.

## Direct edit (escape hatch)

You can still author the values directly — the Oracle is a librarian, not a gatekeeper. Use the direct path when:

- The brief is extremely specific ("primary is exactly `#d92626`").
- A human dev is editing outside the codegen flow.

Just pass the value verbatim — the indexer accepts raw hex / rem / family-stack values for cases that don't fit Crosswind tokens:

```csharp
var theme = new IkonTheme
{
    ["primary"]      = "#d92626",
    ["radius"]       = "0.625rem",
    ["font-heading"] = "Quicksand",
};
```

## Notes

- **`IkonTheme` is a class in `Ikon.Parallax`. DO NOT redefine it.** It's auto-imported via `global using Ikon.Parallax;` in the scaffold's `GlobalUsings.cs`. The platform baseline (fonts, color ramps, radii, motion) is already inside `Ikon.Parallax.Theming.Theme` and inherited automatically — your indexer entries are *overrides* on top of that baseline, not a from-scratch CSS sheet. **Never write `class IkonTheme : ITheme` or `class Theme : ITheme` in the app's source tree** — that's the deleted pattern from before the indexer existed. If `new IkonTheme()` won't compile, the `global using Ikon.Parallax;` line is missing from `GlobalUsings.cs` — fix that, don't reimplement the class.
- **Pass `new IkonTheme { ... }` to `UI`** when STYLING is in the plan. **Never `Theming.Apply(...)`, `Theming.Custom(...)`, or `Theme.Custom(...)`** — those factories were retired; the configurable surface is the `IkonTheme` class with `Mode`, `DarkMode`, plus the indexer.
- **The minimum commitment is small:** `["primary"]` (the whole brand cluster in one line — CTAs, solid fills, hovers, focus rings, brand icons, brand text), the page pair (`["background"]`, `["foreground"]`), `["card"]`, shape (`["radius"]`), and an explicit dark story (`DarkMode` block or `Mode = ThemeMode.Fixed`). With these set, `Button.PrimaryMd`, `Card.Default`, `Layout.Page` all render in the brand palette automatically — including the primary CTA's own fill and its hover, which the cluster covers.
- **Pick brand contrast for light steps.** Text on brand fills defaults to white. Light brand step (≤ 500) → `["primary-foreground"] = "#0A0A0A"`. Dark brand step (≥ 600) → omit.
- **For non-Tailwind palettes**, pass raw hex (`["primary"] = "#ffd54f"`). The resolver passes raw values through unchanged.
- **Don't repeat brand colors in component class arrays.** Once `["primary"]` is set, `Button.PrimaryMd` picks it up everywhere. Hand-rolling `bg-amber-400` per button breaks the brand commitment AND breaks dark theme. (Decorative one-offs are different — they belong inline, per the two-tier rule.)
- **Never write `bg-primary` / `text-primary` / `border-primary` in new code.** Those utilities are legacy neutral tiers (page surface / body text / hairline), not brand — write `bg-background` / `text-foreground` / `border-secondary`. The theme KEY `primary` means brand; the legacy utility classes do not.

## See also

- `typical-app-structure` — the skeleton; pair with this pattern when STYLING is declared.
- `crosswind-styling-and-motion-guide` (top-level guide) — full Crosswind utility class reference.
- `ikon-theming-guide` (top-level guide) — the canonical theme-key reference, value kinds, density, dark contract, mood cookbook.
