# Ikon Theming Guide

## Ikon Theming Guide

The canonical reference for committing a per-app brand mood (palette, fonts, radius, motion) via `new IkonTheme { ... }`. Self-contained — ingest this single doc and produce a coherently-themed app.

---

# Ikon Theming Guide

How to commit a per-app brand mood (palette, fonts, radius, density, motion) on top of the platform's Ikon CSS baseline.

This is the canonical reference for the `IkonTheme` configurable surface. Self-contained — a third-party code generator (Cursor, Codex, Copilot, ChatGPT) can ingest just this doc and produce a coherently-themed Ikon AI App.

## TL;DR

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    // One committed palette, pinned (no OS dark flip). For an adaptive app,
    // drop Fixed and add a DarkMode block instead — see "How dark mode works".
    Mode = ThemeMode.Fixed,

    ["primary"]              = "amber-400",  // whole brand cluster: CTAs, checked controls, focus rings, brand icons + text
    ["primary-foreground"]   = "#0A0A0A",    // text on brand fills — needed because amber-400 is a light step

    ["background"]           = "zinc-950",
    ["foreground"]           = "amber-50",
    ["card"]                 = "zinc-900",
    ["muted-foreground"]     = "zinc-500",
    ["border"]               = "zinc-800",

    ["radius"]               = "rounded-2xl",
    ["density"]              = "comfortable",
    ["font-heading"]         = "Crimson Pro", // literal family name — the Google Fonts import is automatic

    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-out",
});
```

That's it. There are no other entry points. No factory, no fluent builder, no named token properties — just the indexer, plus `Mode` and `DarkMode`.

## Two-tier styling

Ikon styling has exactly two tiers. Knowing which tier a decision belongs to answers most "should this be a token?" questions.

**Tier 1 — the structural core.** A small set of roles (~15: surfaces, text tiers, borders, the brand cluster, radius, fonts, density, motion defaults) committed once in `new IkonTheme { ... }`. This tier is the theme's contract: everything in it re-themes in one place, flips correctly in dark mode (or pins honestly with `Mode = ThemeMode.Fixed`), and reaches every consumer — components, presets, focus rings, native (Flutter) clients.

**Tier 2 — the expressive layer.** Everything with personality — gradients, textures, glows, decorative colors, ornaments — is styled CONCRETELY at the use point with plain Crosswind classes and arbitrary values (`bg-gradient-to-r from-rose-400 to-amber-300`, `bg-[radial-gradient(...)]`, `[text-shadow:0_0_8px_#f0f]`). No token obligation, no naming ceremony. A pirate tavern's brass glow does not need a design system; it needs the right gradient on the right Box.

Governing rules:

1. **The structural core stays small.** Adding a role to the core is an API change, reviewed — not a drive-by. Tier creep is the death mode of two-tier systems.
2. **Expression is concrete and unlimited.** Decorative styling lives at the use point with no token pressure. The app's DESIGN brief is its only contract.
3. **Coherence is reviewed against the app's DESIGN brief, not enforced by token discipline.** Intentional variation the brief calls for is correct by definition.
4. **Tokens are earned, not imposed.** Extract a custom variable only when repetition proves the role (the same concrete value used again and again) — never upfront.
5. **Dark-mode honesty.** Expressive apps usually commit `Mode = ThemeMode.Fixed` — a pirate tavern has no light mode, and pretending otherwise ships broken contrast. Adaptive apps keep dark free via the structural core. Make the call explicitly.

## How it works

`IkonTheme` implements `ITheme`. You pass it as the second arg to `new UI(app, ...)`. Its `Css` getter composes the platform's Ikon CSS baseline with an override block built from your indexer entries.

Every entry commits one or more CSS variables. The renderer dispatches by **key shape**:

| Key shape | Emits | Value resolver |
|---|---|---|
| Theme key from the reference table (`primary`, `card`, `border`, `radius`, `density`, ...) | its canonical variables (one key can commit a whole cluster) | per the key's value kind |
| Tailwind palette step (`amber-400`, `zinc-950`) | `--color-{key}` (families that are also Ikon scales, e.g. `neutral-900`, additionally set `--{key}` so semantic tokens move too) | color |
| Ikon scale step (`brand-500`, `accent-300`, `error-600`) | `--{key}` | color |
| `rounded-{rung}` | `--radius-{rung}` | radius |
| `shadow-{rung}` | `--shadow-{rung}` plus the per-layer `--shadow-{rung}-{1,2}` / `--shadow-{rung}-{1,2}-color` pairs the sized utilities read | box-shadow value (up to two layers), or another rung name to re-point |
| `font-{role}` | `--font-{role}` | family stack; literal family names auto-import from Google Fonts |
| `ease-{kind}` | `--ease-{kind}` | easing |
| any other baseline variable name (`bg-brand-solid`, `text-primary-on-brand`, `spacing`) | `--{key}` | smart sniff |
| `--custom-name` (explicit `--` prefix) | `--custom-name` | raw — a deliberately declared custom variable |
| anything else | `--{key}` plus a one-time production warning (a dead variable nothing reads — almost always a typo) | smart sniff |

The "smart sniff" inspects the **value** to pick a resolver: `<palette>-<step>` → color ref, `rounded-*` → resolved rem, `font-*` → font ref, `duration-*` → milliseconds, easing keyword → cubic-bezier, anything else (hex, rgb, oklch, rem, ms, gradient, raw family name) → unchanged. So `["primary"] = "amber-400"` and `["primary"] = "#F5A524"` both work.

Mistyped keys and mismatched values (`["rounded-lg"] = "amber-400"`) log a warning once per token per process. Silence intentional cases with the `IKON_DEV_WARNINGS=0` environment variable.

## What's NOT in the system

- **Named token properties.** No `Brand = "amber-400"`. Every token override is an indexer entry; `Mode` and `DarkMode` are the only non-indexer members.
- **Auto-contrast.** Setting `["background"] = "zinc-950"` does not auto-pick a light text color — set `["foreground"]` yourself. Text on brand fills defaults to white, so `["primary-foreground"]` is only needed for LIGHT brand steps.
- **Magic value resolution beyond the documented kinds.** `["density"] = "fluffy"` is not a density; the override is skipped with a warning so the baseline unit stands.

Committing a full mood takes roughly 10-14 entries: the brand line, the surface/text lines, shape, density, type, motion.

## How dark mode works

There is ONE contract:

1. **Apps theme dark exclusively through the theme object** — either `DarkMode = new IkonTheme { ... }` (adaptive: light block + dark block) or `Mode = ThemeMode.Fixed` (one committed palette, pinned in both schemes). Never hand-author dark styling per component.
2. **Components that use semantic classes get dark for free.** `bg-background`, `bg-card`, `text-foreground`, `border-secondary`, `bg-brand-solid` and every `Ikon.Parallax.Theming` preset resolve through variables that flip with the active scheme.
3. **`theme-dark:` is the variant for structural dark-only tweaks** (it follows the in-app theme toggle, i.e. `data-theme="dark"`). Rarely needed — reach for it only when a semantic token genuinely can't express the difference.
4. **`dark:` follows the toggle too, plus an OS fallback.** Under the default dual strategy it emits both a rule scoped to the in-app theme (`[data-theme="dark"]` / `.dark`) and a `prefers-color-scheme: dark` fallback that applies only when no explicit theme is set.

Adaptive app — author both palettes in one block:

```csharp
var theme = new IkonTheme
{
    ["primary"]    = "violet-600",
    ["background"] = "stone-50",
    ["foreground"] = "stone-950",
    ["card"]       = "#ffffff",

    DarkMode = new IkonTheme
    {
        ["primary"]    = "violet-300",
        ["background"] = "stone-950",
        ["foreground"] = "stone-50",
        ["card"]       = "stone-900",
    },
};
```

The renderer emits the dark block under `[data-theme="dark"]`, `.dark`, and a `prefers-color-scheme: dark` fallback for pages without an explicit attribute. The in-app toggle (`ClientFunctions.SetThemeAsync(Theme.Dark, targetId: clientSessionId)`) sets `data-theme`.

Fixed app — one palette, no flip:

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["background"] = "#14100b",
    ["foreground"] = "#e8dcc4",
    // ...
};
```

`Fixed` emits the committed values under the dark selectors too, so neither the OS preference nor the toggle changes anything. This is the honest choice for vivid/expressive single-scheme apps. `Mode = ThemeMode.Fixed` together with a `DarkMode` block throws — they contradict each other.

## Theme key reference

### Theme keys (the structural core)

Each key commits the listed canonical CSS variables. One value fans out to the whole set.

| Key | Commits | Effect | Example |
|---|---|---|---|
| `primary` (alias: `brand`) | `--bg-brand-solid`, `--bg-brand-solid-hover`, `--bg-brand-button`, `--bg-brand-button-hover`, `--border-brand`, `--border-focus-ring`, `--fg-brand-primary`, `--text-brand-secondary`, `--text-brand-tertiary` | ALL brand-tinted chrome: CTAs, checked Checkbox/Switch/Radio, focus rings, brand icons, brand-tinted text | `"amber-400"` |
| `primary-foreground` | `--text-brand-button`, `--text-primary-on-brand` | text on brand fills (defaults to white — set only for light brand steps) | `"#0A0A0A"` |
| `background` | `--bg-background` | the page surface | `"zinc-950"` |
| `foreground` | `--text-primary`, `--text-foreground`, `--text-card-foreground`, `--text-popover-foreground` | reading text on every surface (page, cards, popovers) | `"amber-50"` |
| `card` | `--bg-card` | elevated cards | `"zinc-900"` |
| `card-foreground` | `--text-card-foreground` | text on cards | `"amber-50"` |
| `popover` | `--bg-popover` | popovers, menus, dialogs | `"zinc-900"` |
| `popover-foreground` | `--text-popover-foreground` | text on popovers | `"amber-50"` |
| `muted` | `--bg-muted` | subtle fills | `"zinc-800"` |
| `muted-foreground` | `--text-muted-foreground` | secondary/caption text | `"zinc-500"` |
| `accent-foreground` | `--text-accent-foreground` | text on hover surfaces | `"zinc-100"` |
| `border` | `--border-primary`, `--border-secondary` | default hairlines and dividers | `"zinc-800"` |
| `input` | `--border-input` | form control borders | `"zinc-700"` |
| `ring` (alias: `ring-brand`) | `--border-focus-ring` | focus ring only (already inside the `primary` cluster) | `"amber-500"` |
| `text-brand` | `--text-brand-secondary`, `--text-brand-tertiary` | brand-tinted copy only (already inside the `primary` cluster — prefer `primary`) | `"amber-300"` |
| `destructive` | `--bg-error-solid`, `--bg-error-solid-hover`, `--bg-error-button`, `--bg-error-button-hover`, `--border-error` | destructive buttons and error chrome | `"red-600"` |
| `destructive-foreground` | `--text-error-button` | text on destructive fills | `"#ffffff"` |
| `radius` (alias: `radius-base`) | `--radius-base` | every `rounded-*` rung shifts proportionally via `calc()`; reaches Flutter. The DEFAULT ramp is stock Tailwind (sm 4px, md 6px, lg 8px, xl 12px, 2xl 16px, 3xl 24px, 4xl 32px at a 16px root), so Tailwind-authored designs render value-identical without any radius keys | `"rounded-2xl"` |
| `density` (alias: `spacing`) | `--spacing` | the unit every numeric spacing utility multiplies — whole-app whitespace; reaches Flutter | `"airy"` |
| `font-heading`, `font-display`, `font-body`, `font-sans`, `font-serif`, `font-mono` | `--font-{role}` | type roles; literal family names auto-import from Google Fonts; `font-heading` also moves `--font-display` (headings consume the display role); reaches Flutter | `"Crimson Pro"` |
| `motion-duration-base` | `--default-transition-duration` | default speed of every `transition-*` utility | `"200ms"` |
| `ease-default` (alias: `motion-easing-default`) | `--default-transition-timing-function` | default easing of every `transition-*` utility | `"ease-out"` |

Later entries win: to refine one variable inside a cluster, set the key first and the explicit canonical variable after it — `["primary"] = "amber-400"` then `["bg-brand-solid-hover"] = "amber-500"`.

### Value kinds

| Kind | Accepted forms | Examples |
|---|---|---|
| Color | Tailwind palette step, Ikon scale step, hex, `rgb()`/`rgba()`/`hsl()`/`oklch()`/`oklab()`, named color | `amber-400`, `#F5A524`, `oklch(0.72 0.15 60)`, `rgba(255,255,255,0.7)` |
| Font family | role token (`font-sans`, `font-serif`, `font-mono`, `font-display`, `font-heading`, `font-body`) or a literal family name (Google Fonts import is automatic) | `"Crimson Pro"`, `"font-mono"` |
| Radius | `rounded-*` rung (`rounded-none` ... `rounded-4xl`, `rounded-full`) or a raw length | `"rounded-2xl"`, `"1.25rem"` |
| Density | keyword `compact` (0.2rem), `default`/`comfortable` (0.25rem), `airy` (0.3rem), or a rem/px length clamped to 0.15-0.4rem | `"airy"`, `"0.28rem"` |
| Duration | `duration-{n}` token or raw CSS time | `"duration-150"`, `"200ms"` |
| Easing | `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`, or raw `cubic-bezier(...)` | `"ease-out"` |

Palette families: `red`, `orange`, `amber`, `yellow`, `lime`, `green`, `emerald`, `teal`, `cyan`, `sky`, `blue`, `indigo`, `violet`, `purple`, `fuchsia`, `pink`, `rose`, `slate`, `gray`, `zinc`, `neutral`, `stone`; steps `50`-`950`. Ikon scale families: `brand`, `neutral`, `error`, `success`, `warning`, `info`, `accent`.

### Collisions — read this before writing `primary` anywhere

- **Theme KEY `primary` = brand.** `["primary"] = "amber-400"` paints CTAs, focus rings, and brand chrome amber.
- **Utility CLASS `bg-primary` = a neutral page surface** (white in light, near-black in dark), `text-primary` = neutral body text, `border-primary` = a neutral hairline. These are legacy tier names; they render unchanged forever, but **never write `bg-primary` / `text-primary` / `border-primary` in new code** — write `bg-background` / `text-foreground` / `border-secondary` instead. The legacy names collide with the shadcn prior (where `primary` means brand) and reading them costs every future maintainer a double-take.
- **The reverse trap:** `["primary"] = "zinc-950"` intending a dark page background paints every CTA near-black. The page surface key is `["background"]`.
- **Bare `accent` and `secondary` are NOT theme keys** (they log a dev warning). Their shadcn and Ikon meanings genuinely conflict: shadcn `accent` is a hover surface (the `bg-accent` utility), while Ikon's `accent-*` scale drives text-selection tints. Set what you mean explicitly — `["bg-accent"]`, `["bg-secondary"]`, or the `accent-*` scale steps below.

### The accent scale (text selection)

The platform consumes exactly two accent steps: `["accent-300"]` is the light-scheme `::selection` tint and `["accent-800"]` the dark-scheme one. `["accent-500"]` / `["accent-600"]` are published anchor variables for your own `var(--accent-500)` references; nothing in the platform reads them.

### Color palette overrides (re-skin a step app-wide)

```csharp
var theme = new IkonTheme
{
    ["amber-400"] = "#F5A524",
    ["zinc-950"]  = "#0a0a0f",
};
```

Emits `--color-amber-400: #F5A524` and `--color-zinc-950: #0a0a0f`. Every `bg-amber-400`, `text-amber-400`, `border-amber-400` in the app picks them up. Ikon scale steps work the same way — `["neutral-900"] = "#101014"` moves BOTH the raw `bg-neutral-900` utilities and every semantic token derived from the neutral ramp.

### Per-rung radius overrides

```csharp
var theme = new IkonTheme
{
    ["rounded-lg"] = "1.25rem",      // tune one rung
    ["rounded-xl"] = "rounded-3xl",  // re-point one rung at another
};
```

`["radius"]` moves all rungs at once; per-rung overrides are for exceptions.

Shadow rungs are themable per layer: `["shadow-lg"] = "0 2px 4px rgb(0 0 0 / 0.07)"` restyles
every `shadow-lg`, and `["shadow-lg"] = "shadow-xl"` re-points one rung at another. The renderer
splits each layer into geometry and colour variables that the utility composes on the element,
so per-element recoloring — `shadow-lg shadow-red-500` — still wins over the themed colour. A
value may carry at most two layers (Tailwind's own scale never has more); extra layers are
dropped with a warning. A key that is not a rung (`shadow-sticker`) is a plain variable for
`shadow-[var(--shadow-sticker)]`.

### Porting an existing Tailwind design (Replit, Lovable, Base44, v0, hand-written)

Author **Tailwind v4** names in all new code — Crosswind tracks the v4 scale, and the
baseline renders v4 classes at exactly their Tailwind values.

The baseline is value-identical to stock Tailwind for spacing, type, and every radius rung —
classes copied from a Tailwind design render the same geometry here with no theme keys at all.
Most existing sources are v3-era, where several utilities were renamed a step down in v4:
`shadow-sm`→`shadow-xs`, `shadow`→`shadow-sm`, the same one-step-down rename for
`rounded` (all corner forms), `blur`, `drop-shadow`, and `backdrop-blur`, plus
`outline-none`→`outline-hidden`, `ring`→`ring-3`, and removed `*-opacity-*` utilities
becoming slash syntax (`bg-black/50`).

Apply that mapping **as you transcribe** — porting is a rewrite, so the translation
happens in flight and there is nothing to run afterwards; the same goes for any files
copied in verbatim. Verify the result by measurement: run the original next to the
port and diff computed styles on matched elements. Everything else ports verbatim.

### Custom variables (expressive-layer escape hatch)

```csharp
var theme = new IkonTheme
{
    ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)",
};
```

An explicit `--` prefix declares a custom variable on purpose (without it, an unknown key warns as a probable typo). Reference it inline via `bg-[var(--hero-glow)]`. Per the two-tier model, prefer writing decorative values concretely at the use point — mint a variable only when the same value repeats enough to earn a name.

## Density

`["density"]` scales the whole app's whitespace through the `--spacing` unit that every numeric spacing utility multiplies (`p-6` = `calc(var(--spacing) * 6)`).

- Keywords: `compact` = 0.2rem, `default` / `comfortable` = 0.25rem, `airy` = 0.3rem. A raw rem/px length is accepted and clamped to 0.15-0.4rem.
- **What scales:** every numeric spacing utility — `p-*`, `gap-*`, `m-*`, `space-*`, `h-*`, `w-*`, `size-*` — proportionally. Hit targets (`h-10` buttons) scale with the whitespace, which keeps proportions coherent.
- **What does NOT scale:** text sizes, radii, border widths, and arbitrary pixel values (`w-[320px]`, `p-[13px]`). Fixed-px component caps (e.g. toast max width) intentionally stay fixed — they are viewport constraints, not rhythm.
- Works on Flutter: the unit maps to the native spacing unit, so density reaches native clients too.

Brief language maps directly: "generous whitespace", "editorial margins", "breathing room" → `airy`; "dense dashboard", "data-heavy" → `compact`.

## Value vocabulary

Every value is one of:

- **Color palette token** — `<palette>-<step>`, e.g. `amber-400`, `zinc-950`, `rose-600`.
- **Ikon scale token** — `<family>-<step>` for `brand`/`neutral`/`error`/`success`/`warning`/`info`/`accent`, e.g. `brand-500`, `accent-300`.
- **Radius rung** — `rounded-none`, `rounded-xs`, `rounded-sm`, `rounded`, `rounded-md`, `rounded-lg`, `rounded-xl`, `rounded-2xl`, `rounded-3xl`, `rounded-4xl`, `rounded-full`.
- **Font role** — `font-sans`, `font-serif`, `font-mono`, `font-display`, `font-heading`, `font-body`.
- **Density keyword** — `compact`, `default`, `comfortable`, `airy`.
- **Easing keyword** — `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`.
- **Raw CSS** — hex (`#F5A524`), rgb/rgba, hsl/hsla, oklch/oklab, named colors, rems, pixels, durations (`150ms`, `0.2s`), gradients, or any other CSS expression. Pass-through unchanged.
- **Literal font family name** — `Crimson Pro`, `Fraunces`, `JetBrains Mono`. Wrapped in a quoted family stack with system fallbacks, and imported from Google Fonts automatically. The four baseline families (Inter, Poppins, Crimson Pro, JetBrains Mono) are self-hosted woff2 bundled into the app frontend, so they load same-origin and work on networks that block Google Fonts; any other family needs `fonts.googleapis.com`/`fonts.gstatic.com` to be reachable.

## Mood rules

For coherent themes, follow these rules (the StylingOracle's internal taxonomy):

- **Pick one palette family.** Warm-bedtime → amber/rose/stone. Fintech minimal → emerald/zinc. Cyberpunk neon → violet/cyan/zinc. Don't mix amber + cyan + lime in one app — the eye reads it as random.
- **Contrast the page.** Dark background (`zinc-950`, `slate-900`) → light foreground (`zinc-50`, `amber-50`). Light background (`zinc-50`, `stone-100`) → dark foreground (`zinc-950`, `stone-950`).
- **Step conventions.** Dark palettes use 900s/950 for `background`, 50s for `foreground`. Light palettes reverse: 50s/100s for `background`, 950s for `foreground`.
- **Brand contrast:** text on brand fills defaults to white. Light brand step (≤ 500) → set `["primary-foreground"]` to a dark value (`"#0A0A0A"`). Dark brand step (≥ 600) → the default is already right.
- **Match radius to mood.** Sharp / brutalist → `rounded-none`. Modest / SaaS → `rounded-md`. Friendly / bedtime → `rounded-2xl` or higher.
- **Match density to mood.** Editorial / calm / luxury → `airy`. Terminal / data-dense → `compact`.
- **Decide the dark story.** One committed scheme → `Mode = ThemeMode.Fixed`. Adaptive → a `DarkMode` block restating the surface/text/brand keys.

## Mood cookbook

Copy-paste blocks for common moods. Each is mood-coherent — palette, fonts, radius, density, motion all pull in the same direction. Every block commits ONE palette, so it pins it with `Mode = ThemeMode.Fixed`; to make a mood adaptive, drop `Fixed` and add a `DarkMode` block restating the surface/text keys for the other scheme.

### warm-bedtime — cozy, low-stakes, evening reading

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "amber-400",
    ["bg-brand-solid-hover"] = "amber-500",
    ["bg-brand-button-hover"]= "amber-500",
    ["primary-foreground"]   = "#0A0A0A",

    ["background"]       = "zinc-950",
    ["foreground"]       = "amber-50",
    ["card"]             = "zinc-900",
    ["popover"]          = "zinc-900",
    ["muted-foreground"] = "zinc-500",

    ["font-heading"]         = "Crimson Pro",
    ["radius"]               = "rounded-2xl",
    ["motion-duration-base"] = "300ms",
    ["ease-default"]         = "ease-out",
};
```

### cyberpunk-neon — hacker terminal, high contrast, glow

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "violet-400",
    ["bg-brand-solid-hover"] = "violet-300",
    ["bg-brand-button-hover"]= "violet-300",
    ["primary-foreground"]   = "#000000",

    ["background"] = "zinc-950",
    ["foreground"] = "cyan-300",
    ["card"]       = "zinc-900",

    ["accent-300"] = "fuchsia-300",   // text-selection tint follows the mood
    ["accent-800"] = "fuchsia-800",

    ["font-heading"]         = "JetBrains Mono",
    ["font-body"]            = "font-mono",
    ["radius"]               = "rounded-none",
    ["density"]              = "compact",
    ["motion-duration-base"] = "100ms",
    ["ease-default"]         = "linear",
};
```

Scanlines are expressive-layer — concrete at the use point, e.g. a full-bleed overlay Box with `absolute inset-0 bg-[repeating-linear-gradient(to_bottom,transparent_0_2px,rgba(168,85,247,0.05)_2px_4px)] pointer-events-none`.

### editorial-vintage — paper-and-ink, serif, generous margins

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "rose-700",
    ["bg-brand-solid-hover"] = "rose-800",
    ["bg-brand-button-hover"]= "rose-800",

    ["background"] = "stone-100",
    ["foreground"] = "stone-950",
    ["card"]       = "stone-50",

    ["font-heading"]         = "Crimson Pro",
    ["font-body"]            = "Crimson Pro",
    ["radius"]               = "rounded-md",
    ["density"]              = "airy",
    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-in-out",
};
```

### brutalist — high contrast, sharp corners, mono type

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "yellow-300",
    ["bg-brand-solid-hover"] = "yellow-200",
    ["bg-brand-button-hover"]= "yellow-200",
    ["border-brand"]         = "#000000",    // refines one cluster variable — later entry wins
    ["primary-foreground"]   = "#000000",

    ["background"] = "#ffffff",
    ["foreground"] = "#000000",
    ["card"]       = "#ffffff",
    ["border"]     = "#000000",

    ["font-heading"]         = "JetBrains Mono",
    ["font-body"]            = "font-mono",
    ["radius"]               = "rounded-none",
    ["motion-duration-base"] = "0ms",
    ["ease-default"]         = "linear",
};
```

### glassmorphism — soft, translucent, light pastels

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "sky-400",
    ["bg-brand-solid-hover"] = "sky-500",
    ["bg-brand-button-hover"]= "sky-500",

    ["background"] = "slate-50",
    ["foreground"] = "slate-900",
    ["card"]       = "rgba(255, 255, 255, 0.7)",   // raw rgba — translucent

    ["font-heading"]         = "font-sans",
    ["radius"]               = "rounded-3xl",
    ["motion-duration-base"] = "300ms",
    ["ease-default"]         = "ease-out",
};
```

The blur is expressive-layer — put `backdrop-blur-md` (or `backdrop-blur-[12px]`) on the translucent surfaces at the use point.

### pastel — soft, friendly, kids / wellness

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "rose-300",
    ["bg-brand-solid-hover"] = "rose-400",
    ["bg-brand-button-hover"]= "rose-400",
    ["primary-foreground"]   = "#0A0A0A",

    ["background"] = "rose-50",
    ["foreground"] = "stone-900",
    ["card"]       = "#ffffff",

    ["accent-300"] = "emerald-300",   // text-selection tint follows the mood

    ["font-heading"]         = "font-sans",
    ["radius"]               = "rounded-2xl",
    ["density"]              = "airy",
    ["motion-duration-base"] = "250ms",
    ["ease-default"]         = "ease-out",
};
```

### noir-contrast — dark, cinematic, single accent

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "red-500",
    ["bg-brand-solid-hover"] = "red-600",
    ["bg-brand-button-hover"]= "red-600",

    ["background"] = "zinc-950",
    ["foreground"] = "zinc-100",
    ["card"]       = "zinc-900",

    ["font-heading"]         = "font-serif",
    ["radius"]               = "rounded-md",
    ["motion-duration-base"] = "400ms",
    ["ease-default"]         = "ease-in-out",
};
```

### solarpunk — natural, optimistic, earthy

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "emerald-500",
    ["bg-brand-solid-hover"] = "emerald-600",
    ["bg-brand-button-hover"]= "emerald-600",

    ["background"] = "stone-50",
    ["foreground"] = "stone-900",
    ["card"]       = "amber-50",

    ["accent-300"] = "amber-300",   // text-selection tint follows the mood

    ["font-heading"]         = "font-serif",
    ["radius"]               = "rounded-xl",
    ["motion-duration-base"] = "250ms",
    ["ease-default"]         = "ease-out",
};
```

### clean-saas — neutral, professional, default-ish

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "blue-600",
    ["bg-brand-solid-hover"] = "blue-700",
    ["bg-brand-button-hover"]= "blue-700",

    ["background"] = "zinc-50",
    ["foreground"] = "zinc-950",
    ["card"]       = "#ffffff",

    ["font-heading"]         = "font-sans",
    ["radius"]               = "rounded-md",
    ["motion-duration-base"] = "150ms",
    ["ease-default"]         = "ease-out",
};
```

### dark-pro — modern dark, neutral, productivity

```csharp
var theme = new IkonTheme
{
    Mode = ThemeMode.Fixed,
    ["primary"]              = "indigo-500",
    ["bg-brand-solid-hover"] = "indigo-400",
    ["bg-brand-button-hover"]= "indigo-400",

    ["background"] = "zinc-950",
    ["foreground"] = "zinc-100",
    ["card"]       = "zinc-900",

    ["font-heading"]         = "font-sans",
    ["radius"]               = "rounded-lg",
    ["motion-duration-base"] = "150ms",
    ["ease-default"]         = "ease-out",
};
```

## Inline component styling

Once your theme is committed, components consume it via semantic Crosswind utilities:

```csharp
// Brand button — follows ["primary"] and ["primary-foreground"].
view.Button(["bg-brand-solid hover:bg-brand-solid-hover text-primary-on-brand px-6 py-3 rounded-lg font-semibold"],
    "Launch", onClick: async () => { });

// Standard surfaces + text tiers.
view.Box(["bg-card border border-secondary rounded-lg p-6"], content: view => { });
view.Text(["text-foreground"], "Body copy");
view.Text(["text-sm text-muted-foreground"], "Caption");

// Brand-tinted heading.
view.Text(["text-2xl font-bold text-brand-secondary"], "Section Title");

// Custom variable (declared with a -- prefix in the theme).
view.Box(["absolute inset-0 -z-10 bg-[var(--hero-glow)] pointer-events-none"]);
```

The semantic utility set the theme drives: surfaces `bg-background`, `bg-card`, `bg-popover`, `bg-muted`, `bg-accent` (hover surface), `bg-secondary`/`bg-tertiary`; text `text-foreground`, `text-muted-foreground`, `text-card-foreground`, `text-secondary`/`text-tertiary`, `text-primary-on-brand`; brand `bg-brand-solid(-hover)`, `bg-brand-button(-hover)`, `text-brand-secondary`/`-tertiary`, `fg-brand-primary` (icons); borders `border-secondary`, `border-input`, `border-brand`, `ring-ring`/`border-border`; status `bg-destructive`, `text-destructive`, `text-destructive-foreground`, `border-destructive` (plus the full `bg-error-*`/`bg-success-*`/`bg-warning-*` families).

Legacy note: `bg-primary`, `text-primary`, `border-primary`, and `text-primary-foreground` render as neutral tiers (page surface / body text / hairline / dark-on-light text) — supported forever, but do not write them in new code; use `bg-background` / `text-foreground` / `border-secondary`.

Expressive styling — the full color palette (`bg-amber-400`), gradients, arbitrary values, and the motion DSL (`motion-[0:opacity-0,100:opacity-100]`) — goes directly in the class array at the use point. See [Crosswind Styling and Motion Guide](crosswind-styling-and-motion-guide.md) for the full utility reference.

## Common pitfalls

- **Don't pass `new IkonTheme()` (no body) if you have a brand intent.** That uses the platform default — generic dark-zinc. Either commit a real `new IkonTheme { ... }` with overrides or accept the default. Never write `class IkonTheme : ITheme` or `class Theme : ITheme` in the app source — both are provided by `Ikon.Parallax`.
- **Don't try `Theming.Apply(...)`, `Theming.Custom(...)`, `Theme.Custom(...)`.** Those factories were retired. The only configurable surface is `new IkonTheme { ... }`.
- **Light brand step needs `["primary-foreground"]`.** Text on brand fills defaults to white; `["primary"] = "amber-400"` without a dark `primary-foreground` puts white text on a light amber fill — unreadable. Dark brand steps (≥ 600) need nothing.
- **Don't strand text on the wrong background.** `["background"] = "zinc-950"` does not auto-set `["foreground"]`. If you skip it, the platform default (which assumes a light background) renders dark text on your dark background — invisible.
- **Don't repeat brand colors in component class arrays.** Hand-rolling `bg-amber-400` per button instead of `bg-brand-solid` defeats the theme commitment AND breaks dark mode. Semantic classes for structure; concrete values only for expressive decoration.
- **Don't write `bg-primary` / `text-primary` / `border-primary` in new code.** Legacy neutral tiers whose names collide with the shadcn brand reading — write `bg-background` / `text-foreground` / `border-secondary`.
- **`["font-body"] = "font-sans"` is a no-op** (the baseline already points `font-sans` at `font-body`; the circular alias is silently skipped). Omit such lines; set a font role only to change it.
- **`Mode = ThemeMode.Fixed` plus a `DarkMode` block throws.** Fixed commits to one scheme; a dark variant contradicts it. Pick one.
- **Heed the theme warnings in logs.** A key that matches nothing ("matches no known semantic variable or palette step") emits a dead variable — almost always a typo. Prefix with `--` to declare a custom variable on purpose; `IKON_DEV_WARNINGS=0` silences the warnings.

## For code generators without StylingOracle access

Five-step recipe for an external LLM (Cursor, Codex, Copilot, ChatGPT) to theme an Ikon AI App from a user brief:

1. **Read the user brief** — extract mood (warm / cyberpunk / editorial / brutalist / etc.) and any user-named colors / fonts.
2. **Pick a mood from the cookbook** — match the brief to one of the 10 named moods above (warm-bedtime, cyberpunk-neon, editorial-vintage, brutalist, glassmorphism, pastel, noir-contrast, solarpunk, clean-saas, dark-pro). If none fit, build a fresh palette using the rules in *Mood rules* above.
3. **Copy the cookbook block as-is** — paste it as the second arg to `new(app, ...)` at the App's UI declaration site.
4. **Adjust the brand** if the user named a specific color — replace the `["primary"]` value (one line commits the whole brand cluster) and the two `-hover` refinements. Keep `["primary-foreground"]` consistent: light step (≤ 500) → `"#0A0A0A"`; dark step (≥ 600) → omit (white default).
5. **Verify** — the output has ONE `new IkonTheme { ... }` block with an explicit dark story (`Mode = ThemeMode.Fixed` OR a `DarkMode` block, never both), no `Theming.Apply(...)`, no hex unless the user asked for one, a coherent palette family, and no `["font-body"] = "font-sans"` no-op lines.

The generated code goes at the top of the App class, replacing the default bare `new IkonTheme()`:

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    Mode = ThemeMode.Fixed,               // or a DarkMode block for adaptive apps
    ["primary"]              = "amber-400",
    ["primary-foreground"]   = "#0A0A0A",
    ["background"]           = "zinc-950",
    ["foreground"]           = "amber-50",
    ["card"]                 = "zinc-900",
    ["muted-foreground"]     = "zinc-500",
    ["border"]               = "zinc-800",
    ["radius"]               = "rounded-2xl",
    ["density"]              = "comfortable",
    ["font-heading"]         = "Crimson Pro",
    ["motion-duration-base"] = "200ms",
    ["ease-default"]         = "ease-out",
});

// ... rest of the app ...
```
