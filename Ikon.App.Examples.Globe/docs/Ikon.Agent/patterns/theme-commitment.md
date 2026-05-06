# Theme Commitment — Theming.Apply at the App declaration site

When the brief declares a STYLING dimension (mood / palette / typography), the brand commitment lives at the App's UI declaration as a `Theming.Apply(...)` call — NOT in `IkonTheme.cs`'s Css raw-string. Pass Crosswind/Tailwind class names per role (`brand: "amber-400"`, `background: "zinc-950"`, `radiusBase: "rounded-lg"`); every component inherits.

## When to use

Any app whose plan has a STYLING section that names a mood, palette, or typography. If the plan says "fintech minimal" or "vintage editorial" or "retro arcade", the right artifact to edit is the App's UI field — paste a `Theming.Apply(...)` call. No `IkonTheme.cs` edit needed.

## Snippet

```csharp
// Pasted at the top of your App class — the Coder's standard styling step:
private UI UI { get; } = new(app, Theming.Apply(
    brand:           "amber-400",
    background:      "zinc-950",
    foreground:      "zinc-50",
    card:            "zinc-900",
    muted:           "zinc-700",
    fontHeading:     "font-sans",
    fontBody:        "font-sans",
    radiusBase:      "rounded-2xl",
    motionDuration:  "200ms",
    motionEasing:    "ease-out"));
```

The Coder gets the role/value pairs from a `style_mood(brief, notes)` call (the Styling Oracle returns 10-15 coherent tokens) and pastes them as named arguments. Direct authoring works too — the values are Crosswind class names (`amber-400`, `zinc-950`, `rounded-lg`, `font-sans`), so the LLM speaks the same vocabulary it uses in component class arrays.

## How — the Styling Oracle (preferred)

The Coder gets four styling tools wired in:

```
style_mood(brief, notes)              → 10-15 coherent role/value pairs (palette, typography, shape, motion)
                                         → seeds the active mood for follow-up calls
                                         → PROPAGATES (paste into Theming.Apply)
style_token(intent)                   → one mood-coherent role+value (e.g. "danger color")
                                         → PROPAGATES (paste into Theming.Apply)
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

The Oracle returns a list of `{ Role, Value, CustomName?, Rationale }` records. Paste them as named arguments into `Theming.Apply(...)`:

```csharp
private UI UI { get; } = new(app, Theming.Apply(
    brand:        "emerald-500",   /* mint — fintech accent */
    background:   "zinc-50",       /* near-white canvas */
    foreground:   "zinc-950",      /* high-contrast body */
    card:         "white",
    muted:        "zinc-500",
    border:       "zinc-200",
    fontHeading:  "font-sans",
    fontBody:     "font-sans",
    radiusBase:   "rounded-md"));
```

Mid-Coding, when a novel mood-coherent THEME-LEVEL need arises:

```csharp
await style_token(intent: "a danger color used by all destructive buttons");
// → { Role: "custom", Value: "rose-600", CustomName: "danger",
//     Rationale: "muted brick — reads warning without breaking the mint palette" }
```

Add it to the same `Theming.Apply(...)` call:

```csharp
custom: new() {
    ["danger"] = "rose-600",
}
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
| **Theme-level** (propagates) | named arg in `Theming.Apply(...)` | `style_mood`, `style_token`, `style_set` |
| **One-off** (single component) | inline class string in that component's array | `style_class` |

Default to theme-level for: palette colors, font families, base radius, base motion, base shadow. Default to one-off for: gradient text on a hero, a single button's glow, a validation-error shake, a one-component motion stagger.

## Direct edit (escape hatch)

You can still author the values directly — the Oracle is a librarian, not a gatekeeper. Use the direct path when:

- The brief is extremely specific ("primary is exactly `#d92626`").
- A human dev is editing outside the codegen flow.

Just pass the value verbatim — `Theming.Apply` accepts raw hex / rem / family-stack values as fallback for cases that don't fit Crosswind tokens:

```csharp
Theming.Apply(brand: "#d92626", radiusBase: "0.625rem", fontHeading: "Quicksand")
```

## Notes

- **Pass `Theming.Apply(...)` to `UI`** when STYLING is in the plan. **Never `Theme.Custom(...)` or `Theming.Custom(...)`** — those factories were replaced by `Theming.Apply` (which takes named C# parameters with Crosswind values, not a fluent builder closure).
- **At minimum, the Oracle should set: `brand`, `background`, `foreground`, plus typography (`fontHeading`/`fontBody`) and shape (`radiusBase`).** With these set, `Button.Default`, `Card.Default`, `Layout.Page` all render in the brand palette automatically.
- **For dark mode**, pass `darkMode: Theming.Apply(...)` with the dark-theme overrides. The renderer emits `[data-theme="dark"]` / `.dark` / `prefers-color-scheme: dark` selectors.
- **For non-Tailwind palettes**, pass raw hex (`brand: "#ffd54f"`). The resolver passes raw values through unchanged.
- **Don't repeat color in component class arrays.** Once `brand` is set, `Button.Default` (which references `bg-primary`) picks it up. Hand-rolling `bg-zinc-950` per button breaks the brand commitment AND breaks dark theme.

## See also

- `typical-app-structure` — the skeleton; pair with this pattern when STYLING is declared.
- `crosswind-styling-and-motion-guide` (top-level guide) — full Crosswind utility class reference.
- `ikon-theme-customization` (top-level guide) — every CSS variable the platform reads.
