# Tailwind Utility Reference

## Tailwind Utility Class Reference

All Crosswind/Tailwind utility classes — layout, flexbox, grid, spacing, typography, backgrounds, borders, effects, shadows, filters, transforms.

---

﻿# Crosswind Tailwind Spec

Below is a clean, exhaustive list of Tailwind CSS v4.x utility families, organized according to the official documentation structure. For each family, the canonical class pattern(s) are shown, including axis/directional shorthands, plus brief notes where v4 renamed or expanded functionality.

* The categories and families are based on Tailwind's v4 "Styling with utility classes" index, which enumerates every utility family by CSS property.
* Items documented under other pages (e.g. rings under box-shadow, gradient stops under background-image) have been incorporated.
* Where v4 removed or renamed families relative to v3, notes reference the v4 upgrade guide.

> **Notation & patterns**
>
> * `{value}` denotes Tailwind's normal value scale plus arbitrary values via brackets: e.g. `w-[3.75rem]`, `bg-[paint(worklet)]`.
> * `{n}` represents any (non‑negative) integer; v4 supports "any number" for many numeric families like `grid-cols-{n}`.
> * Axes/edges: `x|y|t|r|b|l`, logical block/inline variants where applicable (e.g. scroll snap).
> * Colors accept the normal palette or arbitrary values; transparency uses the `/alpha` suffix (e.g. `bg-red-500/20`). Families like `bg-opacity-*`, `text-opacity-*`, etc. were removed in v4.

---

## 1) Layout

* **Container**: `container` (responsive width constraints + centered)
* **Breaks**: `break-after-{auto|avoid|all|...}`, `break-before-{...}`, `break-inside-{auto|avoid|avoid-page|avoid-column}`
* **Box decoration**: `box-decoration-{clone|slice}`
* **Box sizing**: `box-{border|content}`
* **Display**: `block`, `inline`, `inline-block`, `contents`, `flow-root`, `table`, `inline-table`, `table-caption`, `table-cell`, `table-column`, `table-column-group`, `table-footer-group`, `table-header-group`, `table-row-group`, `table-row`, `list-item`, `hidden`, `flex`, `inline-flex`, `grid`, `inline-grid`
* **Float / Clear**: `float-{left|right|none}`, `clear-{left|right|both|none}`
* **Isolation**: `isolate`, `isolation-auto`
* **Object fit**: `object-{contain|cover|fill|none|scale-down}`
* **Object position**: `object-{bottom|center|left|left-bottom|left-top|right|right-bottom|right-top|top}` or `object-[<position>]`
* **Overflow**: `overflow-{auto|hidden|clip|visible|scroll}`, `overflow-x-*`, `overflow-y-*`
* **Overscroll behavior**: `overscroll-{auto|contain|none}`, `overscroll-x-*`, `overscroll-y-*`
* **Position**: `static`, `fixed`, `absolute`, `relative`, `sticky`
* **Offsets** (inset/edges): `inset-{value}`, `inset-x-{value}`, `inset-y-{value}`, `top-{value}`, `right-{value}`, `bottom-{value}`, `left-{value}`, `start-{value}`, `end-{value}` (supports tokens & arbitrary)
* **Z-index**: `z-{value}` (supports auto, negative values)
* **Visibility**: `visible`, `invisible`, `collapse`
* **Columns**: `columns-{value}`
* **Aspect ratio**: `aspect-{auto|square|video|[ratio]}`

## 2) Flexbox & Grid

* **Flex basis**: `basis-{value}`
* **Flex direction**: `flex-{row|row-reverse|col|col-reverse}`
* **Flex wrap**: `flex-{wrap|wrap-reverse|nowrap}`
* **Order**: `order-{first|last|none|{n}}`
* **Flex shorthand**: `flex-1`, `flex-auto`, `flex-initial`, `flex-none`
* **Grow/Shrink**: `grow`, `grow-0`, `shrink`, `shrink-0`
* **Align content/items/self**:

  * `content-{normal|center|start|end|between|around|evenly|baseline|stretch}`
  * `items-{start|end|center|baseline|stretch}`
  * `self-{auto|start|end|center|stretch|baseline}`
* **Place content/items/self**: `place-content-{...}`, `place-items-{...}`, `place-self-{...}`
* **Justify content/items/self**: `justify-{start|end|center|between|around|evenly}`, `justify-items-{start|end|center|stretch}`, `justify-self-{auto|start|end|center|stretch}`
* **Gap**: `gap-{value}`, `gap-x-{value}`, `gap-y-{value}`
* **Grid templates**: `grid-cols-{n}`, `grid-rows-{n}`
* **Grid lines**: `col-{auto|span-{n}|start-{n}|end-{n}}`, `row-{auto|span-{n}|start-{n}|end-{n}}`
* **Grid auto**: `grid-flow-{row|col|dense|row-dense|col-dense}`, `auto-cols-{min|max|fr|auto|[...]}`, `auto-rows-{min|max|fr|auto|[...]}`

## 3) Spacing

* **Padding**: `p-{value}`, `px-*`, `py-*`, `pt|pr|pb|pl-*`, `ps|pe-*` (logical inline-start/end)
* **Margin**: `m-{value}`, `mx-*`, `my-*`, `mt|mr|mb|ml-*`, `ms|me-*` (logical inline-start/end; supports negative values, e.g. `-mt-2`, `-m-[3px]`)
* **Space between** (siblings): `space-x-{value}`, `space-y-{value}` (+ `space-x-reverse`, `space-y-reverse`).

## 4) Sizing

* **Width**: `w-{value}` (includes `w-{fraction}`, `w-{screen|min|max|fit}`, arbitrary)
* **Min/Max width**: `min-w-{value}`, `max-w-{value}`
* **Height**: `h-{value}` (includes `h-{screen|min|max|fit}`, arbitrary)
* **Min/Max height**: `min-h-{value}`, `max-h-{value}`
* **Square size (both axes)**: `size-{value}`

## 5) Typography

* **Font family**: `font-{sans|serif|mono}` or `font-[...]`
* **Font size**: `text-{xs|sm|base|lg|xl|2xl|...|9xl}` or `text-[...]`
* **Font smoothing**: `antialiased`, `subpixel-antialiased`
* **Font style/stretch/variant**: `italic`, `not-italic`; `font-stretch-{ultra-condensed...ultra-expanded}`; `normal-nums`, `slashed-zero`, `lining-nums`, etc.
* **Font palette (COLRv1)**: `font-palette-{normal|light|dark|match-parent}` or `font-palette-[--custom]`; define palettes with `font-palette-values-[--id font=<family> base=<n> 0=<color> ...]`.
* **Font weight**: `font-{thin|extralight|light|normal|medium|semibold|bold|extrabold|black}`
* **Leading (line-height)**: `leading-{none|tight|snug|normal|relaxed|loose|[number]|[length]}`
* **Lists**: `list-{inside|outside}`, `list-{disc|decimal|...}`, `list-image-[url(...)]`
* **Text align**: `text-{left|center|right|justify|start|end}`
* **Text decoration**:

  * Line: `underline`, `overline`, `line-through`, `no-underline` (`decoration-none` also acceptable)
  * Color: `decoration-{color}`
  * Style: `decoration-{solid|double|dotted|dashed|wavy}`
  * Thickness: `decoration-{from-font|[length]}`
  * Underline offset: `underline-offset-{auto|[length]}`
* **Text indent/overflow/transform/wrap**: `indent-{value}`, `text-{ellipsis|clip}`, `truncate`, `uppercase|lowercase|capitalize|normal-case`, `text-wrap-{wrap|nowrap|balance|pretty}`
* **Line clamp**: `line-clamp-{n}` (multi-line text truncation)
* **Content**: `content-[value]` (sets CSS `content` property, for `before`/`after` pseudo-elements)
* **Vertical align**: `align-{baseline|top|middle|bottom|text-top|text-bottom|sub|super}`
* **Whitespace / word-break / hyphens**: `whitespace-{normal|nowrap|pre|pre-line|pre-wrap|break-spaces}`, `break-{normal|words|all|keep}`, `hyphens-{none|manual|auto}`
* **Text color**: `text-{color}`

## 6) Backgrounds

* **Attachment/clip/origin/position/repeat/size**:
  `bg-{fixed|local|scroll}`, `bg-clip-{border|padding|content|text}`, `bg-origin-{border|padding|content}`,
  `bg-{bottom|center|left|left-bottom|left-top|right|right-bottom|right-top|top}` or `bg-[position]`,
  `bg-{repeat|no-repeat|repeat-x|repeat-y|repeat-round|repeat-space}`,
  `bg-{auto|cover|contain|[length]|[percentage]}`
* **Images (including gradients)**:

  * **Directional gradients**: `bg-linear-to-{t|tr|r|br|b|bl|l|tl}` (aliases `bg-gradient-to-*` remain documented/used)
  * **Angles**: `bg-linear-{angle}` (e.g. `bg-linear-45`)
  * **Radial gradients**: `bg-radial` (default `ellipse at center`) or `bg-radial-[shape_at_position]`
  * **Conic gradients**: `bg-conic` (default `from 0deg`), `bg-conic-{angle}` (e.g. `bg-conic-45`), or `bg-conic-[from_<angle>_<position>]`
  * **Color stops**: `from-{color}`, `via-{color}`, `to-{color}` (+ `via-none`). Stops also accept positions (`from-50%`, `via-70%`, `to-100%`).
  * **Custom images**: `bg-[url('...')]`, etc.
  * **Background image standalone**: `bg-image-{none|[<image>]}` and `bg-position-{...}` / `bg-size-{...}` for setting only the respective sub-properties.
* **Background color**: `bg-{color}`

## 7) Borders (incl. Outline)

* **Border width**: `border-{value}`, `border-x|y|t|r|b|l|s|e-{value}` (+ `divide-x|y-{value}` for sibling dividers; supports reverse modifiers).
* **Border color/style**: `border-{color}`, `border-{solid|dashed|dotted|double|hidden|none}`
* **Border radius**: `rounded`, `rounded-{none|sm|md|lg|xl|2xl|3xl|full}`, edge/corner variants `rounded-{t|r|b|l|s|e}-{...}`, `rounded-{tl|tr|br|bl|ss|se|es|ee}-{...}`
* **Outline**: `outline-*` family — dedicated width/color/style/offset utilities in v4.
  * Width: `outline-{n}` or `outline-[{length}]`, also `outline-width-{...}`
  * Style: `outline-{solid|dashed|dotted|double|none|hidden}`, also `outline-style-{...}`. Width utilities (including bare `outline`) emit `outline-style: var(--tw-outline-style, solid)` alongside `outline-width`, so the outline renders solid on its own, and an explicit style utility (e.g. `outline-dashed`) still wins regardless of class order.
  * Color: `outline-{color}`, also `outline-color-{...}`
  * Offset: `outline-offset-{n}` or `outline-offset-[{length}]`
  * Shorthand reset: `outline-none` and `outline-hidden` both emit `--tw-outline-style: none; outline-style: none`; `outline-hidden` additionally renders a `2px solid transparent` outline with `outline-offset: 2px` under `@media (forced-colors: active)` for a11y.
  * Arbitrary full shorthand: `outline-[2px_dashed_red]` → `outline: 2px dashed red`.

## 8) Effects (shadows, rings, blend, opacity)

* **Box shadow**: `shadow`, `shadow-{2xs|xs|sm|md|lg|xl|2xl}`, `shadow-none`, `shadow-inner`, `shadow-{color}`, plus inset shadows `inset-shadow-{2xs|xs|sm}`, `inset-shadow-none`, `inset-shadow-{color}`
* **Rings** (implemented via shadow):
  `ring-{n}` (width), `ring-{color}`, `ring-inset`, plus inset rings `inset-ring-{n}`, `inset-ring-{color}`, and ring offset: `ring-offset-{n}`, `ring-offset-{color}`.
* **Mix / background blend**: `mix-blend-{mode}`, `bg-blend-{mode}`
* **Opacity** (element): `opacity-{0..100}`
* **Text shadow**: `text-shadow`, `text-shadow-{2xs|xs|sm|md|lg|xl|2xl}`, `text-shadow-none`, `text-shadow-{color}` (added in v4.1; the `xl`/`2xl` sizes are Crosswind extensions beyond Tailwind's scale).

## 9) Filters & Backdrop filters

* **Filter primitives** (auto-emitting `filter`):
  `blur-{xs|sm|md|lg|xl|2xl|3xl|[length]}`, `brightness-{value}`, `contrast-{value}`, `drop-shadow`, `drop-shadow-{xs|sm|md|lg|xl|2xl}`, `drop-shadow-none`, `drop-shadow-{color}`, `grayscale`, `hue-rotate-{deg}`, `invert`, `saturate-{value}`, `sepia`
* **Optional filter toggle**: `filter`, `filter-none`
* **Backdrop primitives**:
  `backdrop-blur-{...}`, `backdrop-brightness-{...}`, `backdrop-contrast-{...}`, `backdrop-grayscale`, `backdrop-hue-rotate-{deg}`, `backdrop-invert`, `backdrop-opacity-{...}`, `backdrop-saturate-{...}`, `backdrop-sepia`
* **Backdrop filter toggle**: `backdrop-filter`, `backdrop-filter-none`

## 10) Tables

* **Border collapse**: `border-{collapse|separate}`
* **Border spacing**: `border-spacing-{value}`, `border-spacing-x-{value}`, `border-spacing-y-{value}`
* **Table layout**: `table-{auto|fixed}`
* **Caption side**: `caption-{top|bottom}`

## 11) Transitions & Animation

* **Transition**: `transition` (all), `transition-none`, `transition-{property}`
* **Timing**: `duration-{ms}`, `ease-{linear|in|out|in-out|[cubic-bezier]}`, `delay-{ms}`
* **Behavior**: `transition-normal`, `transition-discrete` (Tailwind v4 names), plus the explicit `transition-behavior-{normal|allow-discrete}`. The shorthand `transition-behavior-discrete` is also accepted and maps to `allow-discrete`.
* **Animation**: `animate-{spin|ping|pulse|bounce|none|[custom]}`
* **`tailwindcss-animate` enter/exit primitives**: `animate-in` / `animate-out` bind the built-in `enter` / `exit` keyframes; layer shorthands set `--tw-enter-*` / `--tw-exit-*` CSS vars the keyframe consumes:
  * Fade: `fade-in-{n}`, `fade-out-{n}` (0–100 → opacity 0.0–1.0)
  * Zoom: `zoom-in-{n}`, `zoom-out-{n}` (0–100 → scale 0.0–1.0)
  * Spin: `spin-in-{deg}`, `spin-out-{deg}` (rotation, defaults to 30deg)
  * Slide: `slide-in-from-{top|bottom|left|right}{-n}?`, `slide-out-to-{top|bottom|left|right}{-n}?` (spacing scale or `%`)

## 12) Transforms

* **Transform (composite/toggle)**: `transform`, `transform-none`
* **Transform style / GPU promotion**: `transform-3d` (preserve-3d), `transform-flat`, `transform-gpu`, `transform-cpu`
* **Transform box**: `transform-{border|content|fill|stroke|view}` (map to `transform-box: border-box|content-box|fill-box|stroke-box|view-box`)
* **Individual transforms**:
  `scale-{n}`, `scale-x-{n}`, `scale-y-{n}`, `scale-z-{n}`;
  `rotate-{deg}`, `rotate-x-{deg}`, `rotate-y-{deg}`, `rotate-z-{deg}`;
  `translate-{value}`, `translate-x-{value}`, `translate-y-{value}`, `translate-z-{value}`, `-translate-{value}`, `-translate-x-{value}`, `-translate-y-{value}`;
  `skew-x-{deg}`, `skew-y-{deg}`;
  **Transform origin**: `origin-{center|top|top-right|...}`
* **Perspective**: `perspective-{none|dramatic|near|normal|midrange|distant|[length]}`, `perspective-origin-{center|top|top-left|...}`
* **Backface visibility**: `backface-{visible|hidden}`

## 13) Interactivity

* **Accent & caret**: `accent-{color}`, `caret-{color}`
* **Appearance**: `appearance-{none|auto}`
* **Color scheme**: `color-scheme-{light|dark|only-light|only-dark|normal}` (alias `scheme-{...}`)
* **Field sizing**: `field-sizing-{content|fixed}`
* **Pointer & cursor**: `pointer-events-{none|auto}`, `cursor-{pointer|wait|text|move|help|not-allowed|...}`
* **Resize**: `resize`, `resize-{x|y|none}`
* **Scroll**: `scroll-{auto|smooth}`, `scroll-m-{value}`, `scroll-m{t|r|b|l|x|y|s|e}-{value}`, `scroll-p-{value}`, `scroll-p{t|r|b|l|x|y|s|e}-{value}`
* **Scroll snap**: `snap-{none|x|y|both}`, `snap-{mandatory|proximity}`, `snap-align-{start|center|end|none}`, `snap-stop-{normal|always}`
* **Touch action**: `touch-{auto|none|pan-x|pan-y|pinch-zoom|manipulation}`
* **User select**: `select-{none|text|all|auto}`
* **Will change**: `will-change-{auto|scroll|contents|transform|[custom]}`

## 14) SVG

* **Fill**: `fill-{color}` (+ `/alpha`)
* **Stroke**: `stroke-{color}`, `stroke-{n}`
* **Stroke styling**: `stroke-{round|square|butt}`, `stroke-join-{miter|round|bevel}`, `stroke-dasharray-[...]`, `stroke-dashoffset-[...]`

## 15) Accessibility

* **Screen reader only**: `sr-only`, `not-sr-only`
* **Forced colors**: `forced-color-adjust-{auto|none}`

> **Sibling borders & rings live under other pages, but are part of core in v4:**
>
> * **Divide (sibling borders)**: `divide-x-{value}`, `divide-y-{value}` (+ `divide-{color}`, `divide-{style}`, `divide-*-reverse`) — documented under Border width.
> * **Ring utilities** are documented under Box shadow: `ring-*`, `inset-ring-*`, `ring-offset-*`.

## 16) Masks

* **Mask image**: `mask-image-{none}` or `mask-image-[url('...')]`, `mask-image-[linear-gradient(...)]` — sets `mask-image`
* **Mask size**: `mask-size-{auto|cover|contain|[length]}` — sets `mask-size`
* **Mask repeat**: `mask-repeat-{repeat|no-repeat|repeat-x|repeat-y|round|space}`, `mask-no-repeat`, `mask-repeat-x`, `mask-repeat-y`, `mask-repeat-round`, `mask-repeat-space`
* **Mask position**: `mask-position-{center|top|bottom|left|right|[position]}` — sets `mask-position`
* **Mask origin**: `mask-origin-{border|padding|content}` — maps to `border-box`, `padding-box`, `content-box`
* **Mask clip**: `mask-clip-{border|padding|content|text}` — maps to box values or `text`
* **Mask composite**: `mask-composite-{add|subtract|intersect|exclude}` — sets `mask-composite`
* **Composable gradient masks** (v4.1): `mask-linear-{angle}`, `mask-radial`, `mask-radial-at-{position}`, `mask-conic-{angle}`, with stops `mask-{linear|radial|conic}-{from|to}-{value}` and edge fades `mask-{t|r|b|l|x|y}-{from|to}-{value}`; layers combine via `mask-composite: intersect`
* **Mask mode / type**: `mask-mode-{alpha|luminance|match-source}`; `mask-type-{alpha|luminance}` (targets SVG `<mask>` elements, no `-webkit-` prefix)

> All mask utilities automatically emit both standard `mask-*` and vendor-prefixed `-webkit-mask-*` declarations for cross-browser compatibility (except `mask-type-*`, which is SVG-only).



---

# Ikon.Crosswind Public API

namespace Ikon.Crosswind
  sealed class CanvasDesignTokenDocument
    ctor()
    Dictionary<string, CanvasTokenValue<string>> BackdropBlur { get; init; }
    Dictionary<string, Dictionary<string, CanvasTokenValue<string>>> ColorScales { get; init; }
    CanvasEffectTokens Effects { get; init; }
    List<string> Guidelines { get; init; }
    Dictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Radii { get; init; }
    CanvasSemanticColorTokens SemanticColors { get; init; }
    CanvasTypographyTokens Typography { get; init; }
    string? Version { get; init; }
    void Validate()
  static class CanvasDesignTokenLoader
    static CanvasDesignTokenDocument Load(Stream stream)
    static CanvasDesignTokenDocument Load(string json)
    static CanvasDesignTokenDocument LoadFromFile(string path)
  sealed class CanvasEffectTokens
    ctor()
    CanvasModeTokenSet BoxShadows { get; init; }
    CanvasModeTokenSet FocusRing { get; init; }
    CanvasModeTokenSet ShadowPalette { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasModeTokenSet
    ctor()
    Dictionary<string, CanvasTokenValue<string>> Dark { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Light { get; init; }
    void Validate(string category)
  sealed class CanvasSemanticColorTokens
    ctor()
    CanvasModeTokenSet Background { get; init; }
    CanvasModeTokenSet Border { get; init; }
    CanvasModeTokenSet Foreground { get; init; }
    CanvasModeTokenSet Text { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasTokenValue<T>
    ctor()
    string? Description { get; init; }
    T Value { get; init; }
  sealed class CanvasTypographyScale
    ctor()
    string? Description { get; init; }
    string? FontFamily { get; init; }
    string FontSize { get; init; }
    string? LetterSpacing { get; init; }
    string LineHeight { get; init; }
    void Validate(string tokenName)
  sealed class CanvasTypographyTokens
    ctor()
    Dictionary<string, CanvasTypographyScale> Display { get; init; }
    Dictionary<string, CanvasTokenValue<string>> FontFamilies { get; init; }
    Dictionary<string, CanvasTokenValue<int>> FontWeights { get; init; }
    Dictionary<string, CanvasTypographyScale> Text { get; init; }
    void Validate()
  // To take effect, assign an instance to TailwindCustomStyleScope.FlutterTheme and pin that scope via TailwindCustomStyleRegistry.PushScope; the resolver then resolves colour scales and semantic tokens against it instead of the platform baseline. Lookup values may be concrete colours, scale references ("neutral-800"), or other semantic tokens — the resolver chases references and normalizes concrete colours to hex.
  sealed class FlutterThemeSource
    ctor(IReadOnlyDictionary<string, string> scaleHex, IReadOnlyDictionary<string, string> darkSemantic, IReadOnlyDictionary<string, string> lightSemantic, double? radiusBasePx = null, IReadOnlyDictionary<string, double>? radiusPx = null, IReadOnlyDictionary<string, string>? fontFamilies = null, double? spacingUnitPx = null)
    IReadOnlyDictionary<string, string> DarkSemantic { get; }
    // Keyed by role ("body", "display", "heading", …); values are plain family names ("Fraunces"), not CSS font stacks.
    IReadOnlyDictionary<string, string> FontFamilies { get; }
    IReadOnlyDictionary<string, string> LightSemantic { get; }
    // Logical px. Rung values derive from this unless RadiusPx pins a rung explicitly; null means platform default.
    double? RadiusBasePx { get; }
    // Values are logical px, keyed by rung name (e.g. "lg"); a pinned rung overrides the value derived from RadiusBasePx.
    IReadOnlyDictionary<string, double> RadiusPx { get; }
    IReadOnlyDictionary<string, string> ScaleHex { get; }
    // Logical px per spacing unit; scales every numeric spacing utility. Null means platform default (4px).
    double? SpacingUnitPx { get; }
    // Maps colours only (colour scales plus light/dark semantic tokens). Radii, typography, and spacing are NOT mapped and stay at platform defaults unless supplied via the constructor.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  enum TailwindColorContext
    Generic
    Background
    Foreground
    Text
    Border
  sealed class TailwindColorDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? background, IReadOnlyDictionary<string, string>? foreground, IReadOnlyDictionary<string, string>? text, IReadOnlyDictionary<string, string>? border)
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // Keyed "{name}-{step}" (e.g. "red-50") → OKLCH value.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Ordered as first seen in the baseline.
    static IReadOnlyList<string> PaletteNames { get; }
    // Ascending numeric order.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  sealed class TailwindCssVariables
    ctor(IDictionary<string, string> light, IDictionary<string, string> dark, string darkThemeName = "dark")
    IReadOnlyDictionary<string, string> Dark { get; }
    string DarkThemeName { get; }
    IReadOnlyDictionary<string, string> Light { get; }
    string EmitDark()
    string EmitLight()
  // Pin a TailwindCustomStyleScope with PushScope around each compile; lookups prefer the ambient scope and fall back to a process-wide scope for legacy single-app hosts.
  static class TailwindCustomStyleRegistry
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    static IDisposable PushScope(TailwindCustomStyleScope scope)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  // Compilation resolves aliases against the ambient scope pinned by TailwindCustomStyleRegistry.PushScope, falling back to the process-wide scope; pin an instance around a compile so co-hosted apps stay isolated.
  sealed class TailwindCustomStyleScope
    ctor()
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Returns true when the merge added or changed at least one alias — the signal that already-compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  sealed class TailwindFontDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? family, IReadOnlyDictionary<string, string>? weight)
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindFontSize
    ctor(string size, string lineHeight, string? letterSpacing)
    string? LetterSpacing { get; }
    string LineHeight { get; }
    string Size { get; }
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
    void Validate()
  sealed class TailwindThemeDefinition
    ctor(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colorScales, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colors, IReadOnlyDictionary<string, string> boxShadow, IReadOnlyDictionary<string, string> shadowPalette, IReadOnlyDictionary<string, string> focusRing, IReadOnlyDictionary<string, string> borderRadius, IReadOnlyDictionary<string, string> backdropBlur, IReadOnlyDictionary<string, string> fontFamily, IReadOnlyDictionary<string, string> fontWeight, IReadOnlyDictionary<string, TailwindFontSize> fontSize)
    IReadOnlyDictionary<string, string> BackdropBlur { get; }
    IReadOnlyDictionary<string, string> BorderRadius { get; }
    IReadOnlyDictionary<string, string> BoxShadow { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ColorScales { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; }
    IReadOnlyDictionary<string, string> FocusRing { get; }
    IReadOnlyDictionary<string, string> FontFamily { get; }
    IReadOnlyDictionary<string, TailwindFontSize> FontSize { get; }
    IReadOnlyDictionary<string, string> FontWeight { get; }
    IReadOnlyDictionary<string, string> ShadowPalette { get; }
  // flutter:-prefixed classes apply only on the Flutter renderer, web: only on web/CSS, unprefixed on both; the active renderer strips its own marker and drops the other's classes. Variant-group syntax flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class.
  static class TargetVariant
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns the same reference (no copy) when the marker is absent.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web
