# UI API Reference

## UI Component API Reference

The conventions every Ikon.Parallax component signature assumes. The signatures themselves are
generated, one section per namespace — `Ikon.Parallax.Components.Standard` for the components,
`Ikon.Parallax.Theming` for the theme slots — so ask for a component by name and you get the
section that declares it. This page is what those signatures leave unsaid.

### Parameters every component shares

These mean the same thing on every component, so no signature repeats them:

- **`style`** — Crosswind utility classes. Lead with the `"default"` marker or a `Theming.*` composite to merge the component's themed default underneath; without one you get exactly the classes you passed.
- **`styleId`** — a CSS class name applied directly. For exceptional cases; prefer `style`.
- **`key`** — a stable identity to help diffing across renders. For exceptional cases; the builder derives one otherwise.
- **`ariaLabel`** — the accessible name for a control whose visible content cannot supply one. Prefer a visible label.
- **`content`** — a nested build lambda receiving its own `UIView`.
- **`props`** — raw props passed through to the underlying component.

On the components that take them, these are also uniform:

- **`value`** / **`onValueChange`** — the controlled value and its change callback. Pass `value` to control the component yourself.
- **`defaultValue`** — the initial value in uncontrolled mode. Pass this *or* `value`, never both.
- **`forceMount`** — when true, keeps the content in the DOM while hidden (so it can animate out, or be measured).
- **`loop`** — when true, keyboard navigation wraps from the last item back to the first.
