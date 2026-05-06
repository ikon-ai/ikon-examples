# Styling & Motion

## Styling

Style UI with Crosswind (Tailwind-like) utility classes in C# string arrays. Animate with `motion-[keyframes]` syntax for fade-in, slide, glow, shimmer, and per-letter effects. Components are called on the `view` parameter inside `UI.Root()` — no namespace import needed.

```csharp
view.Button([Button.PrimaryMd, "mt-2 w-fit self-center"], label: "Submit");
view.Box([Card.Default, "p-6 mb-4"], content: view => { ... });
```

### Theme Constants & UI Guidelines

**Always use theme constants from `IkonTheme.cs` instead of raw Tailwind utilities.** Never use raw hex colors, Tailwind palette classes (`text-purple-500`, `bg-blue-100`), or hardcoded spacing/shadows/radius values when a theme constant exists.

Key categories:

- **Text**: `Text.Display`, `Text.H1`–`Text.H4`, `Text.Body`, `Text.BodyStrong`, `Text.Label`, `Text.Caption`, `Text.Muted`, `Text.Small`, `Text.Code`, `Text.Link`
- **Button**: `Button.SolidSm/Md/Lg`, `Button.PrimarySm/Md/Lg`, `Button.NeutralSm/Md/Lg`, `Button.OutlineSm/Md/Lg`, `Button.GhostSm/Md/Lg`, `Button.ErrorSm/Md/Lg`, `Button.SuccessSm/Md/Lg`, `Button.WarningSm/Md/Lg`, `Button.InfoSm/Md/Lg`, `Button.LinkSm/Md/Lg`, `Button.Size.Icon`
- **Card**: `Card.Default`, `Card.Subtle`, `Card.Elevated`, `Card.Strong`, `Card.Outline`, `Card.Flat`, `Card.Ghost`, `Card.Interactive`, `Card.InteractiveFill`, `Card.Selected`, `Card.Glass`, `Card.GlassSubtle`, `Card.Header`, `Card.Content`, `Card.Footer`
- **Layout**: `Layout.Row.Xs/Sm/Md/Lg/Xl`, `Layout.Row.SpaceBetween`, `Layout.Column.Xs/Sm/Md/Lg/Xl`, `Layout.Grid.Cols2/Cols3/Cols4`, `Layout.Split.Root/Sidebar/Main/Detail`, `Layout.Page`, `Layout.Section`, `Layout.Center`, `Layout.Stretch`
- **Page**: `Page.Default`, `Page.Plain`, `Page.Gradient`
- **Container**: `Container.Xs` through `Container.Xl7`, `Container.Full`, `Container.Prose`, `Container.Screen`
- **Input**: `Input.Default`, `Input.DefaultSm`, `Input.DefaultLg`, `Input.Ghost`, `Input.Invalid`
- **Badge**: `Badge.GreySm/Md/Lg`, `Badge.BrandSm/Md/Lg`, `Badge.ErrorSm/Md/Lg`, `Badge.SuccessSm/Md/Lg`, `Badge.WarningSm/Md/Lg`, `Badge.InfoSm/Md/Lg`, `Badge.OutlineGrey/Brand/Error/Success/Warning/Info`
- **Icon**: `Icon.Default`, `Icon.Size.Xs/Sm/Md/Lg`, `Icon.Spinner`, `Icon.SpinnerSm`, `Icon.SpinnerLg`
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

Use semantic color tokens (`text-primary`, `bg-card`, `border-secondary`) — never raw hex values. Follow the 60-30-10 rule: 60% `bg-background`, 30% `bg-card`/`bg-secondary`, 10% brand accents.

### Theme Customization

For per-app palette overrides without editing the base theme, use `Theming.Apply(...)` at the top of your app file. Every parameter is a named C# argument that takes a **Crosswind/Tailwind class name** as its value (e.g. `amber-400`, `zinc-950`, `rounded-lg`, `150ms`, `ease-out`) — never a raw hex or CSS string. Set as few or as many as you need; unset roles inherit the base theme.

```csharp
private UI UI { get; } = new(app, Theming.Apply(
    brand: "violet-500",
    background: "slate-950",
    foreground: "slate-50",
    card: "slate-900",
    border: "slate-700",
    fontHeading: "Inter",
    radiusBase: "rounded-lg",
    darkMode: Theming.Apply(brand: "violet-400", background: "slate-950")));
```

The complete set of named parameters:

| Parameter | Value form | Sets |
|---|---|---|
| `brand` | palette step (`amber-400`, `violet-600`) | Primary brand color (drives `bg-brand-solid`, `--brand`, `--primary`, primary buttons, focus rings). Foreground-on-brand auto-derives. |
| `background` | palette step | Page background (`bg-background`, `--background`). |
| `foreground` | palette step | Default text color (`text-primary`, `text-foreground`, `--foreground`). |
| `card` | palette step | Card / popover surface (`bg-card`, `bg-popover`, `--card`, `--popover`). |
| `muted` | palette step | Muted text (`text-muted-foreground`, `--muted`). |
| `accent` | palette step | Secondary accent (`bg-accent`, `text-accent`). |
| `border` | palette step | Default border color (`border-primary`, `border-input`, `--border`). |
| `fontHeading` | font family name | Heading font (`font-heading`, `font-display`). System fallback stack appended automatically. |
| `fontBody` | font family name | Body font (`font-body`, `font-sans`). |
| `radiusBase` | rounded utility (`rounded-none`, `rounded-md`, `rounded-2xl`) | Base radius — all `rounded-*` scales derive from this. |
| `motionDuration` | duration token (`100ms`, `300ms`) | Default transition duration (`--motion-duration`). |
| `motionEasing` | easing keyword (`linear`, `ease-out`, `ease-in-out`) | Default easing (`--motion-easing`). |
| `custom` | `Dictionary<string, string>` | Escape hatch for arbitrary CSS vars (gradient stops, decorative tokens). Keys without a leading `--` get one. |
| `darkMode` | another `Theming.Apply(...)` | Separate dark-mode palette applied under `[data-theme="dark"]`, `.dark`, and `prefers-color-scheme: dark`. |

`Theming.Apply` is the **only** factory. **`Theme.Custom(...)` and `Theming.Custom(...)` were removed** — they were the older fluent-builder API. Do not invent `.GradientBrand` / `.BorderColor` / `.PrimaryColor` / `.SurfaceColor` either; they never existed. For tokens outside the table, use `custom: new() { ["my-token"] = "value" }` and reference it via `bg-[var(--my-token)]`. For Tailwind-style gradients, use the utility classes (`bg-gradient-to-br from-{color} to-{color}`) directly on components.

For deeper customization (full color scales, every radius/shadow token, dark-mode shadows, font fallbacks):

Edit `IkonTheme.cs` to customize the app's visual identity. The top of the CSS section contains the customizable values — colors, radius, shadows, and fonts. Everything below (semantic token mappings, dark mode overrides, C# style tokens) is infrastructure that rarely needs changing.

**Colors** — Six color scales, each with 12 steps (25–950). To change a color, replace all 12 hex values for that scale. Generate scales using OKLCH: keep the seed color's hue and chroma, vary lightness across the steps (97% for step 25 down to 15% for step 950), reduce chroma at extremes.

| Scale | Purpose |
|-------|---------|
| `--brand-*` | Primary accent — buttons, links, active states |
| `--neutral-*` | Backgrounds, borders, text — 60% of the UI |
| `--error-*` | Error states, destructive actions |
| `--success-*` | Success states, confirmations |
| `--warning-*` | Warning states, caution |
| `--info-*` | Informational elements |

**Radius** — Change only `--radius-base`. All other radius tokens derive from it. Sharp (0.25rem) → modern (0.5rem) → friendly (0.75rem) → playful (1rem).

**Shadows** — Light and dark mode shadow definitions. Adjust opacity values for lighter/heavier shadows.

**Fonts** — Four tokens + matching `@import` URLs. `--font-display` for headings, `--font-body` for UI text, `--font-serif` for editorial, `--font-mono` for code. Always keep fallback stacks.

**Style tokens** — The C# static classes (`Button`, `Card`, `Text`, `Layout`, etc.) compose Crosswind utility classes into reusable constants. Use these in style arrays: `[Button.PrimaryMd, "mt-4"]`. You can also write Crosswind classes inline or create custom token variables for app-specific patterns. Using the built-in tokens is not mandatory but ensures visual consistency.

**Generating from an image** — Extract: dominant accent color → brand scale, background tone → neutral scale, corner radius, heading/body font styles. Cross-check that all dimensions reinforce the same aesthetic. Apply in a single pass.

**Aesthetic guidance** — Tint neutrals toward the brand hue. Use OKLCH for perceptually uniform scales. Pair a distinctive display font with a readable body font. Choose radius that matches the emotional tone. Avoid pure black/white, generic AI defaults (neon accents, gradient text), and overused fonts (Inter, Roboto) for display.

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
