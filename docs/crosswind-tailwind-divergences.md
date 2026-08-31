# Crosswind ↔ Tailwind v4: deliberate divergences

Crosswind's Tailwind compatibility is measured by a differential conformance
harness (`Ikon.Crosswind.Test/Conformance/`, run with
`IKON_RUN_TAILWIND_CONFORMANCE=1`) that compiles a ~1030-class corpus through
both Crosswind and the real Tailwind CLI (pinned 4.1.11) and compares output.
As of 2026-08-30 parity is **98.5%** (623 byte-match + 383 semantically
equivalent of 1021; 10 Crosswind-only extensions such as the `animate-in`/
`animate-out` enter/exit utilities and the `theme-*:` variants are excluded). The remaining **15 divergent classes** are all deliberate, listed in
`conformance-allowlist.txt`, and documented here. Anything not on that list
fails CI.

Every divergence below is a product decision, not a gap. If one stops being
worth its trade-off, remove its allowlist entry — the harness then fails until
the emission matches Tailwind.

## The divergences and their reasoning

**1. `rounded-full` family → `9999px` (Tailwind: `calc(infinity * 1px)`)** — 2
classes. `calc(infinity * 1px)` is Chromium 99; an older engine drops the
declaration and every pill squares off. `9999px` is indistinguishable from it
below a 20000px element, which no layout has. Trade-off: none in practice.

**2. Dual `dark:` strategy** — `dark:*`, `not-dark:*`.
Crosswind emits both the `[data-theme="dark"]`/`.dark` selector rule *and* a
`prefers-color-scheme` fallback; Tailwind makes you configure one strategy.
Apps get OS-preference dark mode out of the box *and* an in-app toggle that
overrides it, with zero config. Trade-off: slightly larger CSS; rule-ordering
is covered by tests.

**3. `data-state-open:` → `[data-state="open"]`** — 1 class.
Tailwind reads `data-state-open` as bare attribute presence. Crosswind's web
renderer is Radix-based and Radix communicates state as `data-state="open"`,
so the value form is what actually works with platform components. This
extends, rather than contradicts, Tailwind's `data-*` handling (plain
`data-foo:` presence semantics match Tailwind exactly).

**4. Opacity modifiers → single `color-mix(in oklch)` rule** — 6 classes
(`bg-red-500/50` and kin). Tailwind emits an sRGB `color-mix` fallback plus an
`@supports` oklab upgrade rule per use. Every browser in the platform floor
(Chrome 111+, Safari 16.4+, Firefox 113+ — all 2023) supports the single rule;
the pair exists for older browsers. **Known failure mode:** on an ancient
WebView the declaration is ignored entirely (e.g. `bg-black/50` renders no
background). This is a conscious browser-floor decision — revisit if a
customer platform predates 2023 engines. (Note: where the *composed* systems
need it — ring/shadow color vars — Crosswind emits Tailwind's exact
srgb+oklab pair; this entry covers only the direct opacity-modifier rules.)

**5. `bg-clip-text` superset** — 1 class.
Crosswind additionally sets `color: transparent` and
`-webkit-text-fill-color: transparent`, so gradient text works with one class
instead of a required three-class combination. Trade-off: an app wanting
clipped-but-visible text (rare) must set an explicit text color after it.

**6. Composable-mask standalone defaults** — `mask-linear-45`,
`mask-radial-at-center`. Tailwind's mask utilities only set position/angle
vars and require composing `mask-*-from/to` stops before anything renders;
Crosswind bakes default black-to-transparent stops so each mask utility works
standalone. Composition still works — stops utilities override the defaults.

**7. v3-style `container`** — 1 class.
Crosswind's `container` centers and pads (the v3 ergonomic form); Tailwind v4
emits width-only breakpoints and expects `mx-auto px-*` alongside. A dev
following v4 docs and adding those classes gets the same result (the
additions are idempotent), so the divergence rarely bites and the one-class
form is what app authors want.

## Not a divergence: physical fallbacks beside logical properties

`px`/`py`/`mx`/`my` and `inset-x`/`inset-y` emit the physical longhands
(`padding-left`/`padding-right` and kin) *before* the logical shorthand
Tailwind emits alone. The logical form is Chromium 87, and an engine that
lacks it drops the declaration and the element loses its padding entirely; an
engine that has both takes the later, logical declaration and so stays correct
under RTL. The observable result on any browser that supports Tailwind's own
output is identical, which is why the harness reads these as equivalent rather
than divergent — there is no allowlist entry for them.
