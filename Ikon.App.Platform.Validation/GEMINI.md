@AGENTS.md

# UI & Theme Guidelines

Before writing any UI code or modifying the theme, read these docs:

1. **Ikon Theme Constants** (`docs/public/ikon-theme-constants.md`) — every confirmed constant from `IkonTheme.cs`. If a constant exists here, use it — never use the raw Tailwind utility instead.
2. **Ikon UI Guidelines** (`docs/public/ikon-ui-guidelines.md`) — which constants to use for every UI element, decision order, and verification checklist.
3. **Ikon Theme Customization** (`docs/public/ikon-theme-customization.md`) — how to modify the theme (brand color, neutral, radius, fonts) and how to generate a theme from a text description or image.

## Rules

- All UI is written in C# using the Ikon reactive UI system — never generate HTML, CSS, or React
- All styling uses constants from `IkonTheme.cs` — never invent styles outside of it
- When the user asks to change the look or style of the app, modify `IkonTheme.cs` following the Theme Customization guide
- When the user provides an image or screenshot as a style reference, follow Section 8 of the Theme Customization guide
- When the user describes a personality ("playful", "professional", "luxury" etc.), follow Section 5 of the Theme Customization guide

## When a constant doesn't exist

If you need a style and no matching constant exists in the Theme Constants reference:

1. **Add it to `IkonTheme.cs`** — define the new constant alongside its siblings in the correct class
2. **Then use it** in the UI code

Never patch over an existing constant with raw utility overrides to approximate a missing one — unless the user explicitly asks you to do so.

<!-- ikon-user-content-below -->
