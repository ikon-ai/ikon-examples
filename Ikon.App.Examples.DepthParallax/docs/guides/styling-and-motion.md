# Styling & Motion

## Styling

Style UI with Crosswind (Tailwind-like) utility classes in C# string arrays. Animate with `motion-[keyframes]` syntax for fade-in, slide, glow, shimmer, and per-letter effects. Components are called on the `view` parameter inside `UI.Root()` — no namespace import needed.

```csharp
view.Button([Button.PrimaryMd, "mt-2 w-fit self-center"], label: "Submit");
view.Box([Card.Default, "p-6 mb-4"], content: view => { ... });
```

### Theme Token Shortcuts (optional)

The `Ikon.Parallax.Theming` namespace ships pre-composed token classes that bundle tested defaults — a Crosswind/Tailwind utility string per role/size combo, built from **semantic theme-aware classes** so they auto-flip with light/dark mode and respond to `IkonTheme` overrides. They're **opt-in shortcuts**, not a requirement: any place a token constant would go, you can pass a raw Crosswind utility string instead.

The full catalog:

- **Text**: `Text.Display`, `Text.H1`–`Text.H4`, `Text.Body`, `Text.BodyStrong`, `Text.Label`, `Text.Caption`, `Text.Muted`, `Text.Small`, `Text.Code`, `Text.Link`
- **Button**: `Button.SolidSm/Md/Lg`, `Button.PrimarySm/Md/Lg`, `Button.NeutralSm/Md/Lg`, `Button.OutlineSm/Md/Lg`, `Button.GhostSm/Md/Lg`, `Button.ErrorSm/Md/Lg`, `Button.SuccessSm/Md/Lg`, `Button.WarningSm/Md/Lg`, `Button.InfoSm/Md/Lg`, `Button.LinkSm/Md/Lg`, `Button.Icon`
- **Card**: `Card.Default`, `Card.Subtle`, `Card.Elevated`, `Card.Strong`, `Card.Outline`, `Card.Flat`, `Card.Ghost`, `Card.Interactive`, `Card.InteractiveFill`, `Card.Selected`, `Card.Glass`, `Card.GlassSubtle`, `Card.Header`, `Card.Content`, `Card.Footer`
- **Layout**: `Layout.Row.Xs/Sm/Md/Lg/Xl`, `Layout.Row.SpaceBetween`, `Layout.Column.Xs/Sm/Md/Lg/Xl`, `Layout.Grid.Cols2/Cols3/Cols4`, `Layout.Split.Root/Sidebar/Main/Detail`, `Layout.Page`, `Layout.Section`, `Layout.Center`, `Layout.Stretch`
- **Page**: `Page.Default`, `Page.Plain`, `Page.Base`
- **Container**: `Container.Xs` through `Container.Xl7`, `Container.Full`, `Container.Prose`, `Container.Screen`
- **Input** (single-line `view.TextField`): `Input.Default`, `Input.DefaultSm`, `Input.DefaultLg`, `Input.Ghost`, `Input.Invalid`
- **Textarea** (multi-line `view.TextArea` — a SEPARATE token set; `Input.*` is single-line-height and wrong for a textarea): `Textarea.Default`, `Textarea.DefaultSm`, `Textarea.DefaultLg`, `Textarea.Invalid`
- **Badge**: `Badge.GreySm/Md/Lg`, `Badge.BrandSm/Md/Lg`, `Badge.ErrorSm/Md/Lg`, `Badge.SuccessSm/Md/Lg`, `Badge.WarningSm/Md/Lg`, `Badge.InfoSm/Md/Lg`, `Badge.OutlineGrey/Brand/Error/Success/Warning/Info`
- **Icon**: `Icon.Default`, `Icon.Xs/Sm/Md/Lg`, `Icon.Spinner`, `Icon.SpinnerSm`, `Icon.SpinnerLg`
- **Nav**: `NavigationMenu.*` (Root, List, Trigger, Content, Link, Indicator)
- **Data**: `DataTable.*`, `Progress.*`, `Skeleton.*`, `Media.*`
- **State**: `State.Focusable`, `State.Disabled`, `State.Pressable`, `State.Loading`, `State.Selected`, `State.Invalid`, `State.Readonly`
- **Tokens**: `Tokens.Radius.*`, `Tokens.Shadow.*`, `Tokens.Duration.*`
- **Transition**: `Transition.Fast/Normal/Slow/Slower`, `Transition.Property.*`
- **Overlay styles**: `Dialog.*`, `AlertDialog.*`, `Popover.*`, `Tooltip.*`, `HoverCard.*`, `Toast.*`, `Sheet.*`, `Drawer.*`
- **Form**: `FormField.Root`, `FormField.Label`, `FormField.LabelRequired`, `FormField.HelpText`, `FormField.ErrorText`
- **Nav**: `NavPanel.Base/Filled/Border/Ghost/Divided`, `NavSection.*`, `NavItem.Sm/Md/Lg` + `NavItem.Default/Active/ActiveBrand`
- **Stat**: `StatCard.Root/Header/IconBox*/Label/Value/Trend/TrendVariant.*`, `EmptyState.Root/IconWrap/Title/Description/Actions`
- **Split**: `ResizableSplit.Root/Handle/HandleVertical`
- **Other**: `Tabs.*`, `Slider.*`, `Select.*`, `RadioGroup.*`, `Checkbox.*`, `Switch.*`, `Alert.*`, `Separator.*`, `Avatar.*`

**Default to semantic theme-aware classes** — `text-primary`, `text-foreground`, `text-muted-foreground`, `bg-background`, `bg-card`, `bg-muted`, `bg-brand-solid`, `border-primary`, `border-secondary`, etc. They map through CSS variables that the platform baseline defines for both light and dark modes, so:

- Switching `data-theme="dark"` (or the user's OS preference flipping) re-paints the UI automatically. No code change, no style-array revisit.
- Per-app brand changes from the `IkonTheme` indexer (`["bg-brand-solid"] = "violet-500"`, etc.) propagate to every site that uses the semantic class.

**Hardcoded Tailwind palette classes** (`bg-amber-400`, `text-blue-600`) and **raw hex** (`#F5A524`) are valid and sometimes the right call — a fixed-brand marketing surface, a decorative gradient, a specific look that shouldn't follow the theme. They just **bypass the theming system**, which means if you later add light/dark switching or repaint the brand, every fixed-color site has to be refactored by hand. Mixing them with `Button.GhostMd`/`Button.OutlineMd` on a manually-dark background without setting the theme is the classic footgun — those buttons use `text-primary`, which stays dark in light mode and renders invisible.

The 60-30-10 rule (60% `bg-background`, 30% `bg-card`/`bg-secondary`, 10% brand accents) is a starting balance, not a constraint.

**Coming from shadcn/Tailwind?** This theme uses the **Untitled-UI** vocabulary, where `primary`/`secondary` are emphasis **tiers**, not the brand: `bg-primary`/`bg-secondary` are neutral **surfaces**, `text-primary`/`text-secondary` are **body-text tiers**, and the brand lives in `bg-brand-solid` / `text-brand` / `border-brand`. So shadcn's `bg-primary` (a brand button) maps to Ikon's `bg-brand-solid`, `text-primary-foreground` → `text-primary-on-brand`, `bg-destructive` → `bg-error-solid`, `text-muted-foreground` → `text-tertiary`, generic `border` → `border-secondary`. Don't reach for a bare `bg-primary` expecting a brand colour — use the Ikon name (or `[Button.PrimaryMd]`).

### Theme Customization

For per-app palette overrides without editing the base theme, use `new IkonTheme { ... }` at the top of your app file. **The only configurable surface is the indexer + `DarkMode` property** — there are no named init properties (no `Brand =`, no `Background =`). Every entry sets one CSS variable. Values are Crosswind/Tailwind class names (e.g. `amber-400`, `zinc-950`, `rounded-lg`, `150ms`, `ease-out`) or raw CSS (hex / rem / family stack / gradient). Set as few or as many as you need; unset CSS variables inherit the base theme.

```csharp
private UI UI { get; } = new(app, new IkonTheme
{
    // Brand cluster — every brand-tinted CSS var, set explicitly.
    ["primary"]              = "violet-500",
    ["bg-brand-solid"]       = "violet-500",
    ["bg-brand-solid-hover"] = "violet-600",
    ["text-brand"]           = "violet-500",
    ["border-brand"]         = "violet-500",
    ["primary-foreground"]   = "#ffffff",

    // Surfaces.
    ["background"]   = "slate-950",
    ["text-primary"] = "slate-50",
    ["card"]         = "slate-900",
    ["border-primary"] = "slate-700",

    // Type + shape.
    ["font-heading"] = "Inter",
    ["radius-base"]  = "rounded-lg",

    // Per-token Tailwind overrides (optional).
    ["amber-400"]  = "#F5A524",     // re-skin a Tailwind palette step app-wide
    ["rounded-lg"] = "1.25rem",     // tune one radius rung
    ["hero-glow"]  = "radial-gradient(circle, #F5A52488, transparent 70%)",  // bespoke decorative

    DarkMode = new IkonTheme { ["primary"] = "violet-400", ["background"] = "slate-950" },
});
```

Common CSS variable names (set the ones you want themed; the rest inherit):

| Cluster | Variables |
|---|---|
| Brand | `primary`, `brand`, `bg-brand-solid`, `bg-brand-solid-hover`, `text-brand`, `border-brand`, `ring-brand`, `primary-foreground` |
| Page | `background`, `bg-background`, `text-primary`, `text-foreground` |
| Surfaces | `card`, `popover`, `text-card-foreground`, `text-popover-foreground` |
| Muted | `muted`, `text-muted-foreground`, `text-tertiary`, `text-quaternary` |
| Accent | `accent-500`, `accent-600` |
| Borders | `border-primary`, `border-secondary`, `border-input` |
| Type | `font-heading`, `font-display`, `font-body`, `font-sans`, `font-mono` |
| Shape + motion | `radius-base`, `motion-duration-base`, `ease-default` |

Per-token Tailwind overrides also work — the renderer dispatches by key shape:

| Indexer key form | Example | Effect |
|---|---|---|
| Tailwind palette step | `["amber-400"] = "#F5A524"` | Override what `bg-amber-400` / `text-amber-400` / `border-amber-400` paints app-wide. |
| Radius rung | `["rounded-lg"] = "1.25rem"` | Override one radius rung independently. |
| Shadow rung | `["shadow-lg"] = "0 8px 16px rgba(0,0,0,.18)"` | Override one shadow rung. |
| Font role | `["font-mono"] = "JetBrains Mono"` | Override one font slot. |
| Bespoke token | `["hero-glow"] = "radial-gradient(...)"` | Free CSS variable referenced as `bg-[var(--hero-glow)]`. |

The renderer dispatches by key shape: Tailwind palette step → `--color-{name}-{step}`; `rounded-*` → `--radius-*`; `shadow-*` → `--shadow-*`; `font-*` → `--font-*`; `ease-*` → `--ease-*`. Anything else falls through as `--{key}: {value}` (with smart sniff so Tailwind tokens used as values still resolve).

`new IkonTheme { ... }` is the **only** configurable surface. There is no auto-fan-out and no auto-contrast: setting `["primary"]` does not also set `["bg-brand-solid"]`, and setting `["background"]` does not auto-pick `["text-primary"]`. Spell out each var. Do not invent `.GradientBrand` / `.BorderColor` / `.PrimaryColor` / `.SurfaceColor` either; they never existed. For Tailwind-style gradients, use the utility classes (`bg-gradient-to-br from-{color} to-{color}`) directly on components.

**The platform `IkonTheme` is the only configurable surface — there is no local `IkonTheme.cs` to edit.** The baseline CSS (color scales, radius/shadow tokens, dark-mode overrides, font fallbacks) lives in the platform `Ikon.Parallax.Theming` assembly and is fixed; per-app brand changes go through the indexer above. For an app-specific palette, set the indexer entries you care about — the rest inherit the platform baseline. To re-skin a Tailwind palette step app-wide, set the indexer key for that step (e.g. `["amber-400"] = "#F5A524"`); to swap the brand scale, set the brand-cluster CSS vars (`primary`, `bg-brand-solid`, `text-brand`, `border-brand`, `primary-foreground`). To generate a palette from an image, extract the dominant accent → brand vars, background tone → background var, corner radius → `radius-base`, font choices → `font-heading` / `font-body`.

### Motion Syntax

Motion animations use `motion-[keyframes]` with percentage steps. Combine with `motion-duration-Xms`, `motion-loop`, `motion-per-letter`.

```csharp
// Fade in
view.Box(["motion-[0:opacity-0,100:opacity-100] motion-duration-500ms"], content: view =>
{
    view.Text([Text.Body], "I fade in!");
});

// Slide up + fade in
view.Box(["motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"]);

// Glow pulse (looping)
view.Box(["motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"]);

// Per-letter wave animation (each letter animates independently)
view.Text(["wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] wave:motion-duration-2500ms wave:motion-per-letter wave:motion-loop"], "Hello");

// Per-letter fade-in with stagger delay (letters appear one by one)
view.Text(["motion-[0:opacity-0,100:opacity-100] motion-duration-300ms motion-per-letter motion-letter-delay-60ms"], "Appearing!");

// Per-word animation
view.Text(["motion-[0:opacity-0_translate-y-[10px],100:opacity-100_translate-y-0] motion-duration-500ms motion-per-word motion-letter-delay-100ms"], "Each word slides in");
```

Key: steps are `percentage:utilities`, separated by commas. Within a step, multiple utilities are joined with underscores (`_`). Track prefixes (e.g. `wave:`) scope animations to named tracks.

```csharp
// Shimmer/loading effect — translate a gradient overlay
view.Box(["w-full h-4 rounded bg-muted relative overflow-hidden " +
    "before:content-[''] before:absolute before:inset-0 " +
    "before:bg-[linear-gradient(90deg,transparent,rgba(255,255,255,0.5),transparent)] " +
    "before:w-[200%] " +
    "before:shimmer:motion-[0:translate-x-[-50%],100:translate-x-[0%]] " +
    "before:shimmer:motion-duration-1000ms before:shimmer:motion-ease-linear before:shimmer:motion-loop"]);

// Scale + blur entrance
view.Box(["motion-[0:opacity-0_scale-[0.5]_blur-[4px],100:opacity-100_scale-100_blur-0] motion-duration-500ms"]);
```

All motion styles are applied inside `UI.Root(content: view => { ... })` — the `view` parameter is a `UIView` builder, not a separate namespace. Example in app context:

```csharp
public async Task Main()
{
    UI.Root([Page.Default], content: view =>
    {
        view.Column(["h-screen items-center justify-center gap-4"], content: view =>
        {
            // Animated heading with fade-in + slide
            view.Text([Text.H2, "motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"], "Welcome!");

            // Pulsing glow button
            view.Button([Button.PrimaryMd, "motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"],
                label: "Click me");
        });
    });
}
```

### Style Organization

For large apps, organize styles in a static class:

```csharp
internal static class Styles
{
    public static readonly string[] PageContainer = [Container.Xl2, "py-8 px-4 min-h-screen"];
    public static readonly string[] MainCard = [Card.Default, Layout.Column.Lg, "p-10 w-full"];
}
```
