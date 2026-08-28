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
    static CanvasDesignTokenDocument LoadFromFile(string path)
    static CanvasDesignTokenDocument LoadFromJson(string json)
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
    required T Value { get; init; }
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
  // To take effect, assign an instance to TailwindCustomStyleScope.FlutterTheme and pin that scope via TailwindCustomStyleRegistry.PushScope; the resolver then resolves colour scales and semantic tokens against it instead of the platform baseline. Lookup values may be concrete colours (#hex, rgb(), hsl(), oklch()), scale references ("neutral-800"), or other semantic tokens — the resolver chases references and normalizes concrete colours to hex. Construct with the object-initializer form, which names each map (new FlutterThemeSource { ScaleColors = …, LightSemantic = …, DarkSemantic = … }); ScaleColors, LightSemantic, and DarkSemantic share a dictionary type, so a positional form would let a transposition of the light and dark maps compile and silently invert the two modes. Each unset map defaults to empty.
  sealed class FlutterThemeSource
    ctor()
    // Dark-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values take the same forms as LightSemantic.
    IReadOnlyDictionary<string, string> DarkSemantic { get; init; }
    // Keyed by role ("body", "display", "heading", …); values are plain family names ("Fraunces"), not CSS font stacks.
    IReadOnlyDictionary<string, string> FontFamilies { get; init; }
    // Light-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are colour literals in a form the resolver can normalize (#hex, rgb(), hsl(), oklch()), scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> LightSemantic { get; init; }
    // Logical px. Rung values derive from this unless RadiusPx pins a rung explicitly; null means platform default. Must be a finite value above zero.
    double? RadiusBasePx { get; init; }
    // Values are logical px, keyed by rung name (e.g. "lg"); a pinned rung overrides the value derived from RadiusBasePx. Every value must be finite and non-negative.
    IReadOnlyDictionary<string, double> RadiusPx { get; init; }
    // Colour-scale entries keyed by "scale-shade" ("neutral-800"). Values are the raw colour strings copied verbatim from the tokens, in any form the resolver can normalize: #rrggbb, rgb()/rgba(), hsl()/hsla() or oklch(). Other CSS colour forms are dropped on Flutter.
    IReadOnlyDictionary<string, string> ScaleColors { get; init; }
    // Logical px per spacing unit; scales every numeric spacing utility. Null means platform default (4px). Must be a finite value above zero.
    double? SpacingUnitPx { get; init; }
    // Maps colours only (colour scales plus light/dark semantic tokens). Radii, typography, and spacing are NOT mapped and stay at platform defaults unless supplied via the object initializer.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  // The split form that makes shadow rungs themable without losing per-element recoloring. A sized shadow utility reads each layer's geometry and colour from separate variables (--shadow-{rung}-{n} / --shadow-{rung}-{n}-color) and composes them on the element, where --tw-shadow-color from shadow-red-500 can still take precedence.
  static class ShadowLayers
    // The --tw-shadow value a sized rung utility emits: every layer read from its split variables with the stock value as fallback, colour deferring to --tw-shadow-color.
    static string Compose(string rung)
    // True when name is a sized Tailwind rung (sm, lg, …) whose utility reads the split variables; none and bespoke names are not.
    static bool IsRung(string name)
    // Pads or truncates to MaxLayers.
    static IReadOnlyList<ShadowLayers.Layer> Pad(IReadOnlyList<ShadowLayers.Layer> layers)
    // The stock layers of a rung, padded with Empty to MaxLayers.
    static IReadOnlyList<ShadowLayers.Layer> RungDefaults(string rung)
    // Splits a box-shadow value into layers, taking the colour of each layer to be its first token that is neither inset nor a length; a layer without one gets currentcolor.
    static IReadOnlyList<ShadowLayers.Layer> Split(string value)
    static string VariableName(string rung, int layer, bool color)
    // The layer a rung emits for a slot its value does not fill: zero geometry, so it stays invisible even when recoloured.
    static readonly ShadowLayers.Layer Empty
    // Layers a rung carries. Tailwind's own scale never exceeds two; a theme value with more is truncated.
    const int MaxLayers = 2
  readonly record struct ShadowLayers.Layer
    ctor(string Geometry, string Color)
    string Color { get; init; }
    string Geometry { get; init; }
  enum TailwindColorContext
    // Untyped context (rings, shadows, gradients). The only context that falls back to the union of all aliases — background, foreground, text, and border merged — when the name is not found in a family-scoped map.
    Generic
    // Family-scoped to background aliases only; unlike Generic, it does not fall back to the merged union.
    Background
    // Family-scoped to foreground aliases only; unlike Generic, it does not fall back to the merged union.
    Foreground
    // Family-scoped to text aliases only; unlike Generic, it does not fall back to the merged union.
    Text
    // Family-scoped to border aliases only; unlike Generic, it does not fall back to the merged union.
    Border
  // Custom colour alias maps split by role. Construct with the object-initializer form (new TailwindColorDefinitions { Background = …, Text = … }); the four maps share a dictionary type, so a positional form would let a transposition of any two compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindColorDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    // Intentionally empty: Tailwind's stock palette has no separate dark root-variable set. A theme's dark appearance comes entirely from the dark overrides the app passes to TailwindCssVariables, merged onto this empty base — an app that emits dark CSS must supply its own dark values rather than expecting a baseline to fall back on.
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // Keyed "{name}-{step}" (e.g. "red-50") → OKLCH value.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Ordered as first seen in the baseline.
    static IReadOnlyList<string> PaletteNames { get; }
    // Ascending numeric order.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  // Light and dark CSS variable maps for a compiled theme, each merged over the Tailwind baseline. Construct with the object-initializer form (new TailwindCssVariables { Light = …, Dark = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently invert the emitted light/dark CSS. An omitted map defaults to the baseline alone.
  sealed class TailwindCssVariables
    ctor()
    // CSS variables for the dark theme, merged over the Tailwind dark baseline. Keys are bare variable names, exactly as for Light.
    IReadOnlyDictionary<string, string> Dark { get; init; }
    string DarkThemeName { get; init; }
    // CSS variables for the light theme, merged over the Tailwind light baseline. Keys are bare variable names ("color-primary", not "--color-primary"); a leading -- is stripped so a CSS-style key still overrides the baseline entry.
    IReadOnlyDictionary<string, string> Light { get; init; }
    // Emits the dark theme variable block under DarkThemeName, or an empty string when there are no dark variables.
    string EmitDark()
    // Emits the :root variable block for the light theme followed by TailwindCssBaseline.AdditionalCss (the keyframes and animation rules the utilities depend on). Use EmitLight with false when the caller composes the baseline CSS itself, otherwise every keyframe is emitted twice.
    string EmitLight()
    string EmitLight(bool includeBaselineCss)
  // Pin a TailwindCustomStyleScope with PushScope around each compile; lookups prefer the ambient scope and fall back to a process-wide scope for legacy single-app hosts.
  static class TailwindCustomStyleRegistry
    // Flutter theme data of the scope active for the current compile, preferring the ambient scope like the alias lookups do.
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    // Makes the given scope the ambient alias source for the current async flow until the returned handle is disposed.
    static IDisposable PushScope(TailwindCustomStyleScope scope)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  // Compilation resolves aliases against the ambient scope pinned by TailwindCustomStyleRegistry.PushScope, falling back to the process-wide scope; pin an instance around a compile so co-hosted apps stay isolated.
  sealed class TailwindCustomStyleScope
    ctor()
    // Optional Flutter theme data derived from the same app theme as the alias definitions. The Flutter style resolver reads it through the ambient scope so each app in a shared process renders its own brand colors on native clients.
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Returns true when the merge added or changed at least one alias — the signal that already-compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  // Custom font family and weight alias maps. Construct with the object-initializer form (new TailwindFontDefinitions { Family = …, Weight = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindFontDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
    void Validate()
  // flutter:-prefixed classes apply only on the Flutter renderer, web: only on web/CSS, unprefixed on both; the active renderer strips its own marker and drops the other's classes. Variant-group syntax flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class.
  static class TargetVariant
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns the same reference (no copy) when the marker is absent.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web
