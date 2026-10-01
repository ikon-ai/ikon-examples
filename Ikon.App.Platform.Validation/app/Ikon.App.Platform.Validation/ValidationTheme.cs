/// <summary>
/// The Ikon brand identity from <c>brand/ikon-brand.md</c>: one electric-magenta accent on
/// warm paper and ink surfaces, the palette Studio ships. Every value goes through the
/// <see cref="IkonTheme"/> indexer, never global CSS, so native clients receive it through the
/// Flutter theme bridge as well as the web.
/// </summary>
public static class ValidationTheme
{
    public static IkonTheme Brand { get; } = new()
    {
        ["brand-25"] = "#fff0f6",
        ["brand-50"] = "#ffe1ee",
        ["brand-100"] = "#ffc3dd",
        ["brand-200"] = "#ff97c2",
        ["brand-300"] = "#ff5ba0",
        ["brand-400"] = "#ff3d8f",
        ["brand-500"] = "#f5277f",
        ["brand-600"] = "#db176e",
        ["brand-700"] = "#b3105a",
        ["brand-800"] = "#8a0f48",
        ["brand-900"] = "#661037",
        ["brand-950"] = "#3d0a20",

        ["neutral-50"] = "#fafafa",
        ["neutral-100"] = "#f4f4f5",
        ["neutral-200"] = "#e8e8ea",
        ["neutral-300"] = "#d4d4d7",
        ["neutral-400"] = "#9a9aa0",
        ["neutral-500"] = "#6e6e74",
        ["neutral-600"] = "#4c4c51",
        ["neutral-700"] = "#38383c",
        ["neutral-800"] = "#2a2926",
        ["neutral-900"] = "#1d1c1a",
        ["neutral-950"] = "#151412",

        // Success leans teal so "good" never competes with the magenta.
        ["success-25"] = "#f0fdfa",
        ["success-50"] = "#d7faf2",
        ["success-100"] = "#aff3e6",
        ["success-200"] = "#76e7d3",
        ["success-300"] = "#38d3bd",
        ["success-400"] = "#16b8a6",
        ["success-500"] = "#0d9d90",
        ["success-600"] = "#0a7d76",
        ["success-700"] = "#0b635f",
        ["success-800"] = "#0c4f4c",
        ["success-900"] = "#0a403e",
        ["success-950"] = "#042625",

        // The platform reads these two accent steps only as the text-selection tint.
        ["accent-300"] = "brand-200",
        ["accent-800"] = "brand-800",

        // Hover is a half-step with the chroma held: a full rung down to brand-700 reads wine,
        // and brand-500 fails AA for the white label.
        ["bg-brand-solid-hover"] = "#ce0266",
        ["bg-brand-button-hover"] = "#ce0266",

        ["radius"] = "0.25rem",

        // Elevation is hairline; depth comes from flat colour and borders, not blur.
        ["shadow-2xs"] = "0 1px 1px rgb(0 0 0 / 0.03)",
        ["shadow-xs"] = "0 1px 1px rgb(0 0 0 / 0.04)",
        ["shadow-sm"] = "0 1px 2px rgb(0 0 0 / 0.05)",
        ["shadow-md"] = "0 1px 2px rgb(0 0 0 / 0.06)",
        ["shadow-lg"] = "0 2px 4px rgb(0 0 0 / 0.07)",
        ["shadow-xl"] = "0 4px 8px rgb(0 0 0 / 0.08)",
        ["shadow-2xl"] = "0 8px 16px rgb(0 0 0 / 0.12)",

        // Paper, not screen white: cards sit one clear step up off it, and selected rows stay a
        // neutral step rather than a brand wash.
        ["background"] = "#f3f1ec",
        ["bg-primary"] = "#f3f1ec",
        ["bg-primary-alt"] = "#f3f1ec",
        ["bg-primary-hover"] = "#ebe8e1",
        ["bg-surface"] = "#ebe8e1",
        ["muted"] = "#ebe8e1",
        ["bg-secondary"] = "#ffffff",
        ["bg-tertiary"] = "#e4e0d8",
        ["bg-brand-selected"] = "#e4e0d8",
        ["text-foreground"] = "var(--text-secondary)",

        DarkMode = new IkonTheme
        {
            // The baseline lightens the dark-mode brand fill; the brand keeps brand-600 with a
            // white label on both grounds, the only step that passes on each.
            ["bg-brand-solid"] = "brand-600",
            ["bg-brand-button"] = "brand-600",
            ["bg-brand-solid-hover"] = "#ce0266",
            ["bg-brand-button-hover"] = "#ce0266",
            ["text-brand-button"] = "#ffffff",
            ["border-brand"] = "brand-400",

            // Warm ink surfaces that read as black across a room and as ink beside paper.
            ["background"] = "#151412",
            ["bg-primary"] = "#151412",
            ["bg-primary-alt"] = "#151412",
            ["bg-primary-hover"] = "#151412",
            ["bg-surface"] = "#1d1c1a",
            ["bg-secondary"] = "#1d1c1a",
            ["bg-secondary-hover"] = "#2a2926",
            ["card"] = "#22211e",
            ["popover"] = "#22211e",
            ["muted"] = "#22211e",
            ["bg-tertiary"] = "#2a2926",
            ["bg-tertiary-hover"] = "#35332f",
            ["bg-quaternary"] = "#35332f",
            ["bg-accent"] = "#2a2926",
            ["bg-active"] = "#2a2926",
            ["bg-brand-selected"] = "#2a2926",
            ["bg-brand-section"] = "#1d1c1a",
            ["bg-brand-section-subtle"] = "#151412",
            ["bg-transparent-hover"] = "#2a2926",
            ["bg-disabled"] = "#2a2926",
            ["bg-disabled-subtle"] = "#151412",
            ["bg-neutral-muted"] = "#151412",
            ["bg-overlay"] = "#151412",

            ["border"] = "#2a2926",
            ["border-primary"] = "#35332f",
            ["border-tertiary"] = "#2a2926",
            ["input"] = "#2a2926",
            ["border-disabled"] = "#35332f",
            ["border-disabled-subtle"] = "#2a2926",

            ["foreground"] = "#f7f7f7",
            ["text-foreground"] = "var(--text-secondary)",
            ["text-secondary"] = "#d3d5d9",
            ["text-secondary-hover"] = "#ececed",
            ["text-secondary-foreground"] = "#f7f7f7",
            ["text-tertiary"] = "#aeb2b8",
            ["text-tertiary-hover"] = "#cecfd2",
            ["text-quaternary"] = "#9ea2a9",
            ["muted-foreground"] = "#aeb2b8",
            ["accent-foreground"] = "#f7f7f7",
            ["text-disabled"] = "#61656c",
            ["text-placeholder"] = "#61656c",
            ["text-placeholder-subtle"] = "#373a41",
            ["fg-primary"] = "#ffffff",
            ["fg-secondary"] = "#cecfd2",
            ["fg-secondary-hover"] = "#ececed",
            ["fg-tertiary"] = "#aeb2b8",
            ["fg-tertiary-hover"] = "#cecfd2",
            ["fg-quaternary"] = "#373a41",
            ["fg-quaternary-hover"] = "#61656c",

            ["shadow-2xs"] = "0 1px 1px rgb(0 0 0 / 0.3)",
            ["shadow-xs"] = "0 1px 1px rgb(0 0 0 / 0.35)",
            ["shadow-sm"] = "0 1px 2px rgb(0 0 0 / 0.4)",
            ["shadow-md"] = "0 1px 2px rgb(0 0 0 / 0.45)",
            ["shadow-lg"] = "0 2px 4px rgb(0 0 0 / 0.5)",
            ["shadow-xl"] = "0 4px 8px rgb(0 0 0 / 0.5)",
            ["shadow-2xl"] = "0 8px 16px rgb(0 0 0 / 0.55)",
        },
    };
}
