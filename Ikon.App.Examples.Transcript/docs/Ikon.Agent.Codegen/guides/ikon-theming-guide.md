# Ikon Theming Guide

## Ikon Theming Guide

The canonical reference for committing a per-app brand mood (palette, fonts, radius, motion) via `new IkonTheme { ... }`. Self-contained — ingest this single doc and produce a coherently-themed app.

---

# Ikon Theming Guide

How to commit a per-app brand mood (palette, fonts, radius, motion) on top of the platform's Ikon CSS baseline.

This is the canonical reference for the `IkonTheme` configurable surface. Self-contained — a third-party code generator (Cursor, Codex, Copilot, ChatGPT) can ingest just this doc and produce a coherently-themed Ikon AI App.

## TL;DR

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    // Brand commitment — set every brand-tinted CSS var explicitly.
    ["primary"]              = "amber-400",
    ["bg-brand-solid"]       = "amber-400",
    ["bg-brand-solid-hover"] = "amber-500",
    ["text-brand"]           = "amber-400",
    ["border-brand"]         = "amber-400",
    ["primary-foreground"]   = "#0A0A0A",   // pick contrast yourself

    // Page surfaces.
    ["background"]   = "zinc-950",
    ["text-primary"] = "amber-50",
    ["card"]         = "zinc-900",

    // Type + shape + motion.
    ["font-heading"]         = "Crimson Pro",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-2xl",
    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-out",

    // Per-token palette overrides (optional).
    ["amber-400"]  = "#F5A524",
    ["rounded-lg"] = "1.25rem",
    ["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",

    // Bespoke decorative token (referenced inline via bg-[var(--hero-glow)]).
    ["hero-glow"]  = "radial-gradient(circle, #F5A52488, transparent 70%)",
});
```

That's it. There are no other entry points. No factory, no fluent builder, no named init properties — just the indexer.

## How it works

`IkonTheme` implements `ITheme`. You pass it as the second arg to `new UI(app, ...)`. Its `Css` getter composes the platform's Ikon CSS baseline with an override block built from your indexer entries.

Every entry is one CSS variable. The renderer dispatches by **key shape**:

| Key shape | Emits | Value resolver |
|---|---|---|
| Color palette step (`amber-400`, `zinc-950`) | `--color-{key}` | `ResolveColor` |
| `rounded-{rung}` | `--radius-{rung}` | `ResolveRadius` |
| `shadow-{rung}` | `--shadow-{rung}` | (raw passthrough) |
| `font-{role}` | `--font-{role}` | `ResolveFontFamily` |
| `ease-{kind}` | `--ease-{kind}` | `ResolveEasing` |
| anything else | `--{key}` | smart sniff |

The "smart sniff" fall-through inspects the **value** to pick a resolver:

- Looks like `<palette>-<step>` → `ResolveColor` → `var(--color-*)` ref
- Starts with `rounded-` → `ResolveRadius` → resolved rem
- Starts with `font-` → `ResolveFontFamily` → `var(--font-*)` ref or quoted family stack
- Easing keyword → `ResolveEasing` → cubic-bezier
- Anything else (hex, rgb, oklch, rem, ms, gradient, raw family name) → unchanged

That means `["primary"] = "amber-400"` and `["primary"] = "#F5A524"` both work — the renderer picks the right form for the value you wrote.

## What's NOT in the system

There's no:

- **Named init properties.** No `Brand = "amber-400"`. Every override is an indexer entry.
- **Fan-out from one role to many CSS vars.** Setting `["primary"]` does not also set `["bg-brand-solid"]`. You spell out each var you want themed.
- **Auto-contrast.** Setting `["background"] = "zinc-950"` does not auto-pick a light text color. You set `["text-primary"]` explicitly.
- **Magic value resolution beyond Crosswind tokens.** `["radius-base"] = "rounded-2xl"` works (Crosswind token resolved); `["radius-base"] = "fluffy"` would emit literal `--radius-base: fluffy` which is invalid CSS.

This is by design. The cost is verbosity — committing a brand takes ~10-15 entries. The benefit is that there's nothing hidden: what you write is what lands in the CSS.

## CSS variable reference

The platform CSS consumes these variables. Set the ones you want to override; the rest stay at the baseline.

### Brand cluster

| Var | Consumed by |
|---|---|
| `primary` | `bg-primary`, `bg-brand-solid` (button fill), focus rings |
| `brand` | brand-tinted backgrounds |
| `bg-brand-solid` | primary button fill |
| `bg-brand-solid-hover` | primary button hover fill |
| `bg-brand-section` | brand-tinted section background |
| `bg-brand-primary` / `bg-brand-secondary` | brand-tinted boxes |
| `text-brand` | brand-colored text |
| `text-brand-primary` / `text-brand-secondary` / `text-brand-tertiary` | tiered brand text |
| `border-brand` | brand borders |
| `border-brand-secondary` / `border-brand-alt` | tiered brand borders |
| `ring-brand` | focus ring color |
| `primary-foreground` | text color on top of `bg-primary` (you pick the contrast) |

### Page surfaces

| Var | Consumed by |
|---|---|
| `background` | `bg-background` (page) |
| `bg-background` | alias |
| `text-primary` | `text-primary` body text |
| `text-foreground` | `text-foreground` |
| `foreground` | alias |
| `card` | `bg-card` (cards, popovers) |
| `popover` | `bg-popover` |
| `text-card-foreground` | text on card surfaces |
| `text-popover-foreground` | text on popover surfaces |

### Muted / accent / border

| Var | Consumed by |
|---|---|
| `muted` | `bg-muted` |
| `text-muted-foreground` | `text-muted-foreground` |
| `text-tertiary` / `text-quaternary` | tiered muted text |
| `accent-500` / `accent-600` | secondary accent |
| `border-primary` / `border-secondary` | default borders |
| `border-input` | input borders |

### Type + shape + motion

| Var | Consumed by |
|---|---|
| `font-heading` | heading font, `font-heading`, `font-display` |
| `font-display` | alias |
| `font-body` | body font |
| `font-sans` / `font-mono` / `font-serif` | font slots |
| `radius-base` | base radius — derived rungs (`--radius-sm` … `--radius-3xl`) shift proportionally via `calc()` |
| `motion-duration-base` | default transition duration |
| `ease-default` | default easing |
| `motion-easing-default` | alias |

### Color palette overrides (re-skin a step app-wide)

```csharp
["amber-400"] = "#F5A524",
["zinc-950"]  = "#0a0a0f",
```

Emits `--color-amber-400: #F5A524` and `--color-zinc-950: #0a0a0f`. Every `bg-amber-400`, `text-amber-400`, `border-amber-400` in the app picks them up.

### Per-rung radius / shadow overrides

```csharp
["rounded-lg"] = "1.25rem",      // tune one rung
["rounded-xl"] = "rounded-3xl",  // re-point one rung at another
["shadow-lg"]  = "0 8px 16px rgba(0,0,0,.18)",
```

### Bespoke decorative tokens

```csharp
["hero-glow"]        = "radial-gradient(circle, #F5A52488, transparent 70%)",
["scanline-overlay"] = "linear-gradient(to bottom, transparent 50%, rgba(255,213,79,0.04) 50%)",
```

Emit as `--hero-glow: …` and `--scanline-overlay: …`. Reference inline via `bg-[var(--hero-glow)]` / `bg-[var(--scanline-overlay)]`.

## Value vocabulary

Every value is one of:

- **Color palette token** — `<palette>-<step>` where palette is one of `red`, `orange`, `amber`, `yellow`, `lime`, `green`, `emerald`, `teal`, `cyan`, `sky`, `blue`, `indigo`, `violet`, `purple`, `fuchsia`, `pink`, `rose`, `slate`, `gray`, `zinc`, `neutral`, `stone` and step is one of `50`, `100`, `200`, `300`, `400`, `500`, `600`, `700`, `800`, `900`, `950`. Examples: `amber-400`, `zinc-950`, `rose-600`.
- **Radius rung** — `rounded-none`, `rounded-xs`, `rounded-sm`, `rounded`, `rounded-md`, `rounded-lg`, `rounded-xl`, `rounded-2xl`, `rounded-3xl`, `rounded-4xl`, `rounded-full`. Resolves to a rem value.
- **Font role** — `font-sans`, `font-serif`, `font-mono`, `font-display`, `font-heading`, `font-body`. Resolves to a `var(--font-*)` ref.
- **Easing keyword** — `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`. Resolves to cubic-bezier.
- **Raw CSS** — hex (`#F5A524`), rgb / rgba (`rgb(255 165 0)`), hsl / hsla, oklch / oklab, named colors (`black`, `white`, `transparent`, `currentColor`), rems (`0.5rem`, `1.25rem`), pixels (`16px`), durations (`150ms`, `0.2s`), gradients (`radial-gradient(...)`, `linear-gradient(...)`), or any other CSS expression. Pass-through unchanged.
- **Literal font family name** — `Crimson Pro`, `Inter`, `JetBrains Mono`. Wraps in a quoted family stack with system fallbacks.

## Mood rules

For coherent themes, follow these rules (the StylingOracle's internal taxonomy):

- **Pick one palette family.** Warm-bedtime → amber/rose/stone. Fintech minimal → emerald/zinc. Cyberpunk neon → violet/cyan/zinc. Don't mix amber + cyan + lime in one app — the eye reads it as random.
- **Contrast the page.** Dark background (`zinc-950`, `slate-900`, etc.) → light foreground (`zinc-50`, `amber-50`). Light background (`zinc-50`, `stone-100`) → dark foreground (`zinc-950`, `stone-950`).
- **Step conventions.** Dark briefs use 900s/950 for `background`, 50s for `text-primary`. Light briefs reverse: 50s/100s for `background`, 950s for `text-primary`.
- **Pick brand contrast yourself.** `["primary"] = "amber-400"` does NOT auto-set `["primary-foreground"]`. Light brand step (≤ 500) → dark foreground (`#0A0A0A`). Dark brand step (≥ 600) → light foreground (`#ffffff`).
- **Match radius to mood.** Sharp / brutalist → `rounded-none`. Modest / SaaS → `rounded-md`. Friendly / bedtime → `rounded-2xl` or higher.

## Mood cookbook

Copy-paste blocks for common moods. Each is mood-coherent — palette, fonts, radius, motion all pull in the same direction.

### warm-bedtime — cozy, low-stakes, evening reading

```csharp
new IkonTheme
{
    ["primary"]              = "amber-400",
    ["bg-brand-solid"]       = "amber-400",
    ["bg-brand-solid-hover"] = "amber-500",
    ["text-brand"]           = "amber-300",
    ["border-brand"]         = "amber-500",
    ["primary-foreground"]   = "#0A0A0A",

    ["background"]   = "zinc-950",
    ["text-primary"] = "amber-50",
    ["card"]         = "zinc-900",
    ["popover"]      = "zinc-900",
    ["text-tertiary"] = "zinc-500",

    ["font-heading"]         = "Crimson Pro",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-2xl",
    ["motion-duration-base"] = "300ms",
    ["ease-default"]         = "ease-out",
}
```

### cyberpunk-neon — hacker terminal, high contrast, glow

```csharp
new IkonTheme
{
    ["primary"]              = "violet-400",
    ["bg-brand-solid"]       = "violet-400",
    ["bg-brand-solid-hover"] = "violet-300",
    ["text-brand"]           = "violet-300",
    ["border-brand"]         = "violet-500",
    ["primary-foreground"]   = "#000000",

    ["background"]   = "zinc-950",
    ["text-primary"] = "cyan-300",
    ["card"]         = "zinc-900",

    ["accent-500"]   = "fuchsia-400",
    ["accent-600"]   = "fuchsia-500",

    ["font-heading"]         = "JetBrains Mono",
    ["font-body"]            = "font-mono",
    ["radius-base"]          = "rounded-none",
    ["motion-duration-base"] = "100ms",
    ["ease-default"]         = "linear",

    ["scanline-overlay"] = "linear-gradient(to bottom, transparent 50%, rgba(168,85,247,0.05) 50%)",
}
```

### editorial-vintage — paper-and-ink, serif, generous margins

```csharp
new IkonTheme
{
    ["primary"]              = "rose-700",
    ["bg-brand-solid"]       = "rose-700",
    ["bg-brand-solid-hover"] = "rose-800",
    ["text-brand"]           = "rose-700",
    ["border-brand"]         = "rose-700",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "stone-100",
    ["text-primary"] = "stone-950",
    ["card"]         = "stone-50",

    ["font-heading"]         = "Crimson Pro",
    ["font-body"]            = "Crimson Pro",
    ["radius-base"]          = "rounded-md",
    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-in-out",
}
```

### brutalist — high contrast, sharp corners, mono type

```csharp
new IkonTheme
{
    ["primary"]              = "yellow-300",
    ["bg-brand-solid"]       = "yellow-300",
    ["bg-brand-solid-hover"] = "yellow-200",
    ["text-brand"]           = "yellow-300",
    ["border-brand"]         = "#000000",
    ["primary-foreground"]   = "#000000",

    ["background"]   = "#ffffff",
    ["text-primary"] = "#000000",
    ["card"]         = "#ffffff",
    ["border-primary"] = "#000000",

    ["font-heading"]         = "JetBrains Mono",
    ["font-body"]            = "font-mono",
    ["radius-base"]          = "rounded-none",
    ["motion-duration-base"] = "0ms",
    ["ease-default"]         = "linear",
}
```

### glassmorphism — soft, translucent, light pastels

```csharp
new IkonTheme
{
    ["primary"]              = "sky-400",
    ["bg-brand-solid"]       = "sky-400",
    ["bg-brand-solid-hover"] = "sky-500",
    ["text-brand"]           = "sky-600",
    ["border-brand"]         = "sky-300",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "slate-50",
    ["text-primary"] = "slate-900",
    ["card"]         = "rgba(255, 255, 255, 0.7)",   // raw rgba — translucent

    ["font-heading"]         = "font-sans",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-3xl",
    ["motion-duration-base"] = "300ms",
    ["ease-default"]         = "ease-out",

    ["card-blur"] = "backdrop-blur(12px)",
}
```

### pastel — soft, friendly, kids / wellness

```csharp
new IkonTheme
{
    ["primary"]              = "rose-300",
    ["bg-brand-solid"]       = "rose-300",
    ["bg-brand-solid-hover"] = "rose-400",
    ["text-brand"]           = "rose-500",
    ["border-brand"]         = "rose-300",
    ["primary-foreground"]   = "#0A0A0A",

    ["background"]   = "rose-50",
    ["text-primary"] = "stone-900",
    ["card"]         = "#ffffff",

    ["accent-500"]   = "emerald-300",
    ["accent-600"]   = "emerald-400",

    ["font-heading"]         = "font-sans",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-2xl",
    ["motion-duration-base"] = "250ms",
    ["ease-default"]         = "ease-out",
}
```

### noir-contrast — dark, cinematic, single accent

```csharp
new IkonTheme
{
    ["primary"]              = "red-500",
    ["bg-brand-solid"]       = "red-500",
    ["bg-brand-solid-hover"] = "red-600",
    ["text-brand"]           = "red-400",
    ["border-brand"]         = "red-600",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "zinc-950",
    ["text-primary"] = "zinc-100",
    ["card"]         = "zinc-900",

    ["font-heading"]         = "font-serif",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-md",
    ["motion-duration-base"] = "400ms",
    ["ease-default"]         = "ease-in-out",
}
```

### solarpunk — natural, optimistic, earthy

```csharp
new IkonTheme
{
    ["primary"]              = "emerald-500",
    ["bg-brand-solid"]       = "emerald-500",
    ["bg-brand-solid-hover"] = "emerald-600",
    ["text-brand"]           = "emerald-700",
    ["border-brand"]         = "emerald-500",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "stone-50",
    ["text-primary"] = "stone-900",
    ["card"]         = "amber-50",

    ["accent-500"]   = "amber-500",
    ["accent-600"]   = "amber-600",

    ["font-heading"]         = "font-serif",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-xl",
    ["motion-duration-base"] = "250ms",
    ["ease-default"]         = "ease-out",
}
```

### clean-saas — neutral, professional, default-ish

```csharp
new IkonTheme
{
    ["primary"]              = "blue-600",
    ["bg-brand-solid"]       = "blue-600",
    ["bg-brand-solid-hover"] = "blue-700",
    ["text-brand"]           = "blue-700",
    ["border-brand"]         = "blue-600",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "zinc-50",
    ["text-primary"] = "zinc-950",
    ["card"]         = "#ffffff",

    ["font-heading"]         = "font-sans",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-md",
    ["motion-duration-base"] = "150ms",
    ["ease-default"]         = "ease-out",
}
```

### dark-pro — modern dark, neutral, productivity

```csharp
new IkonTheme
{
    ["primary"]              = "indigo-500",
    ["bg-brand-solid"]       = "indigo-500",
    ["bg-brand-solid-hover"] = "indigo-400",
    ["text-brand"]           = "indigo-400",
    ["border-brand"]         = "indigo-500",
    ["primary-foreground"]   = "#ffffff",

    ["background"]   = "zinc-950",
    ["text-primary"] = "zinc-100",
    ["card"]         = "zinc-900",

    ["font-heading"]         = "font-sans",
    ["font-body"]            = "font-sans",
    ["radius-base"]          = "rounded-lg",
    ["motion-duration-base"] = "150ms",
    ["ease-default"]         = "ease-out",
}
```

## Dark mode

Set `DarkMode = new IkonTheme { ... }` to author light AND dark palettes in one block:

```csharp
new IkonTheme
{
    ["primary"]      = "violet-600",
    ["background"]   = "stone-50",
    ["text-primary"] = "stone-950",
    ["card"]         = "#ffffff",

    DarkMode = new IkonTheme
    {
        ["primary"]      = "violet-300",
        ["background"]   = "stone-950",
        ["text-primary"] = "stone-50",
        ["card"]         = "stone-900",
    },
}
```

The renderer emits the dark block under `[data-theme="dark"]`, `.dark`, and `@media (prefers-color-scheme: dark)`.

## Inline component styling

Once your theme is committed, components consume it via standard Crosswind utilities:

```csharp
// Brand button — picks up your `primary` and `primary-foreground`.
view.Button(["bg-primary text-primary-foreground px-6 py-3 rounded-lg font-semibold hover:bg-brand-solid-hover"],
    "Launch", onClick: ...);

// Brand-tinted box.
view.Box(["bg-brand-section rounded-lg p-6 border border-brand"], content: ...);

// Brand-colored heading.
view.Text(["text-2xl font-bold text-brand-primary"], "Section Title");

// Bespoke decorative token.
view.Box(["absolute inset-0 -z-10 bg-[var(--hero-glow)] pointer-events-none"]);
```

The full color palette (`bg-amber-400`, `text-zinc-50`, etc.) and motion DSL (`motion-[0:opacity-0,100:opacity-100]`) are also available. See [Crosswind Styling and Motion Guide](crosswind-styling-and-motion-guide.md) for the full utility reference.

## Common pitfalls

- **Don't pass `new IkonTheme()` (no body) if you have a brand intent.** That uses the platform default — generic dark-zinc. Either commit a real `new IkonTheme { ... }` with overrides or accept the default. Never write `class IkonTheme : ITheme` or `class Theme : ITheme` in the app source — both are provided by `Ikon.Parallax`.
- **Don't try `Theming.Apply(...)`, `Theming.Custom(...)`, `Theme.Custom(...)`.** Those factories were retired. The only configurable surface is `new IkonTheme { ... }`.
- **Don't forget `primary-foreground`.** Setting `["primary"] = "amber-400"` won't auto-pick a contrasting text color. You set `["primary-foreground"] = "#0A0A0A"` (light brand step) or `"#ffffff"` (dark brand step) yourself.
- **Don't strand text on the wrong background.** Setting `["background"] = "zinc-950"` does not auto-set `["text-primary"]`. If you skip the text-primary override, the platform default (which assumes a light background) renders dark text on your dark background — invisible.
- **Don't repeat brand colors in component class arrays.** Hand-rolling `bg-amber-400` per button instead of `bg-brand-solid` defeats the theme commitment AND breaks dark mode. Use the semantic aliases.

## For code generators without StylingOracle access

Five-step recipe for an external LLM (Cursor, Codex, Copilot, ChatGPT) to theme an Ikon AI App from a user brief:

1. **Read the user brief** — extract mood (warm / cyberpunk / editorial / brutalist / etc.) and any user-named colors / fonts.
2. **Pick a mood from the cookbook** — match the brief to one of the 10 named moods above (warm-bedtime, cyberpunk-neon, editorial-vintage, brutalist, glassmorphism, pastel, noir-contrast, solarpunk, clean-saas, dark-pro). If none fit, build a fresh palette using the rules in *Mood rules* above.
3. **Copy the cookbook block as-is** — paste it as the second arg to `new(app, ...)` at the App's UI declaration site.
4. **Adjust the brand** if the user named a specific color — replace every `["primary"] = "..."`, `["bg-brand-solid"] = "..."`, `["text-brand"] = "..."`, `["border-brand"] = "..."` line with the user's choice. Keep `primary-foreground` consistent (light step → `#0A0A0A`; dark step → `#ffffff`).
5. **Verify** — the output should have a `new IkonTheme { ... }` block, no `Theming.Apply(...)`, no hex unless the user asked for one, and the palette family stays coherent (no `amber-400` paired with `cyan-500`).

The generated code goes at the top of the App class, replacing the default bare `new IkonTheme()`:

```csharp
[App]
public class MyApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme
    {
        // … paste cookbook block here, adjust brand if user named a color …
    });

    // … rest of the app …
}
```
