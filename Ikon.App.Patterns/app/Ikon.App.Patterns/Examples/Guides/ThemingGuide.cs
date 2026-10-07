namespace Ikon.App.Patterns.Examples;

// The theming guide's mood gallery, as code that compiles.
//
// Every one of these fences was a bare `new IkonTheme { … }` — an expression belonging to no
// declaration, so none of them could be compiled anywhere. Each is now the assignment a reader
// writes, which is also what makes the palette keys checkable: a key the theme does not define
// silently does nothing.
static class ThemingGalleryExamples
{
    public static IkonTheme ThemingHowDarkModeWorks()
    {
        #region example:theming-how-dark-mode-works
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingHowDarkModeWorks2()
    {
        #region example:theming-how-dark-mode-works-2
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["background"] = "#14100b",
            ["foreground"] = "#e8dcc4",
            // ...
        };
        #endregion

        return theme;
    }

    public static IkonTheme ThemingWarmBedtimeCozyLowStakesEveningReading()
    {
        #region example:theming-warm-bedtime-cozy-low-stakes-evening-reading
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingCyberpunkNeonHackerTerminalHighContrastGlow()
    {
        #region example:theming-cyberpunk-neon-hacker-terminal-high-contrast-glow
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingEditorialVintagePaperAndInkSerifGenerousMargins()
    {
        #region example:theming-editorial-vintage-paper-and-ink-serif-generous-margins
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingBrutalistHighContrastSharpCornersMonoType()
    {
        #region example:theming-brutalist-high-contrast-sharp-corners-mono-type
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingGlassmorphismSoftTranslucentLightPastels()
    {
        #region example:theming-glassmorphism-soft-translucent-light-pastels
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["primary"]              = "sky-400",
            ["bg-brand-solid-hover"] = "sky-500",
            ["bg-brand-button-hover"]= "sky-500",
            ["primary-foreground"]   = "#0A0A0A",

            ["background"] = "slate-50",
            ["foreground"] = "slate-900",
            ["card"]       = "rgba(255, 255, 255, 0.7)",   // raw rgba — translucent

            ["font-heading"]         = "font-sans",
            ["radius"]               = "rounded-3xl",
            ["motion-duration-base"] = "300ms",
            ["ease-default"]         = "ease-out",
        };
        #endregion

        return theme;
    }

    public static IkonTheme ThemingPastelSoftFriendlyKidsWellness()
    {
        #region example:theming-pastel-soft-friendly-kids-wellness
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingNoirContrastDarkCinematicSingleAccent()
    {
        #region example:theming-noir-contrast-dark-cinematic-single-accent
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["primary"]              = "red-600",
            ["bg-brand-solid-hover"] = "red-700",
            ["bg-brand-button-hover"]= "red-700",

            ["background"] = "zinc-950",
            ["foreground"] = "zinc-100",
            ["card"]       = "zinc-900",

            ["font-heading"]         = "font-serif",
            ["radius"]               = "rounded-md",
            ["motion-duration-base"] = "400ms",
            ["ease-default"]         = "ease-in-out",
        };
        #endregion

        return theme;
    }

    public static IkonTheme ThemingSolarpunkNaturalOptimisticEarthy()
    {
        #region example:theming-solarpunk-natural-optimistic-earthy
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["primary"]              = "emerald-600",
            ["bg-brand-solid-hover"] = "emerald-700",
            ["bg-brand-button-hover"]= "emerald-700",

            ["background"] = "stone-50",
            ["foreground"] = "stone-900",
            ["card"]       = "amber-50",

            ["accent-300"] = "amber-300",   // text-selection tint follows the mood

            ["font-heading"]         = "font-serif",
            ["radius"]               = "rounded-xl",
            ["motion-duration-base"] = "250ms",
            ["ease-default"]         = "ease-out",
        };
        #endregion

        return theme;
    }

    public static IkonTheme ThemingCleanSaasNeutralProfessionalDefaultIsh()
    {
        #region example:theming-clean-saas-neutral-professional-default-ish
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
        #endregion

        return theme;
    }

    public static IkonTheme ThemingDarkProModernDarkNeutralProductivity()
    {
        #region example:theming-dark-pro-modern-dark-neutral-productivity
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["primary"]              = "indigo-600",
            ["bg-brand-solid-hover"] = "indigo-500",
            ["bg-brand-button-hover"]= "indigo-500",

            ["background"] = "zinc-950",
            ["foreground"] = "zinc-100",
            ["card"]       = "zinc-900",

            ["font-heading"]         = "font-sans",
            ["radius"]               = "rounded-lg",
            ["motion-duration-base"] = "150ms",
            ["ease-default"]         = "ease-out",
        };
        #endregion

        return theme;
    }
}
// The theming guide's remaining fences.
//
// Two of them declare the app's own `UI` property, so each needs a class of its own; the three
// palette-override fences used to be single initializer LINES, which belong to no declaration and
// compile nowhere — each is the whole initializer now, which is what a reader copies anyway.

file sealed class ThemeCommittedExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:theming-committed-palette
    private UI UI { get; } = new(app, new IkonTheme
    {
        // One committed palette, pinned (no OS dark flip). For an adaptive app,
        // drop Fixed and add a DarkMode block instead — see "How dark mode works".
        Mode = ThemeMode.Fixed,

        ["primary"]              = "amber-400",  // whole brand cluster: CTAs, checked controls, focus rings, brand icons + text
        ["primary-foreground"]   = "#0A0A0A",    // text on brand fills — pins the near-black label amber-400 would get anyway

        ["background"]           = "zinc-950",
        ["foreground"]           = "amber-50",
        ["card"]                 = "zinc-900",
        ["muted-foreground"]     = "zinc-500",
        ["border"]               = "zinc-800",

        ["radius"]               = "rounded-2xl",
        ["density"]              = "comfortable",
        ["font-heading"]         = "Crimson Pro", // literal family name — a baseline family the frontend already ships

        ["motion-duration-base"] = "200ms",
        ["ease-default"]         = "ease-out",
    });
    #endregion

    public void Use() => Log.Instance.Debug($"{UI}");
}

file sealed class ThemeWholeAppExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:theming-whole-app
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
    #endregion

    public void Use() => Log.Instance.Debug($"{UI}");
}

static class ThemingOverridesExamples
{
    public static IkonTheme PaletteSteps()
    {
        #region example:theming-palette-steps
        var theme = new IkonTheme
        {
            ["amber-400"] = "#F5A524",
            ["zinc-950"]  = "#0a0a0f",
        };
        #endregion

        return theme;
    }

    public static IkonTheme RadiusRungs()
    {
        #region example:theming-radius-rungs
        var theme = new IkonTheme
        {
            ["rounded-lg"] = "1.25rem",      // tune one rung
            ["rounded-xl"] = "rounded-3xl",  // copy another rung's stock size
        };
        #endregion

        return theme;
    }

    public static IkonTheme CustomVariable()
    {
        #region example:theming-custom-variable
        var theme = new IkonTheme
        {
            ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)",
        };
        #endregion

        return theme;
    }

    public static void UsingTheTokens(UIView view)
    {
        #region example:theming-using-tokens
        // Brand button — follows ["primary"] and ["primary-foreground"].
        view.Button(["bg-brand-solid hover:bg-brand-solid-hover text-primary-on-brand px-6 py-3 rounded-lg font-semibold"],
            "Launch", onClick: async () => { });

        // Standard surfaces + text tiers.
        view.Box(["bg-card border border-secondary rounded-lg p-6"], content: view => { });
        view.Text(["text-foreground"], "Body copy");
        view.Text(["text-sm text-muted-foreground"], "Caption");

        // Brand-tinted heading.
        view.Text(["text-2xl font-bold text-brand-secondary"], "Section Title");

        // Custom variable (declared with a -- prefix in the theme); image: routes a gradient to background-image.
        view.Box(["absolute inset-0 -z-10 bg-[image:var(--hero-glow)] pointer-events-none"]);
        #endregion
    }
}
