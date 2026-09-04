// The theming guide's mood gallery, as code that compiles.
//
// Every one of these fences was a bare `new IkonTheme { … }` — an expression belonging to no
// declaration, so none of them could be compiled anywhere. Each is now the assignment a reader
// writes, which is also what makes the palette keys checkable: a key the theme does not define
// silently does nothing.
file static class DocThemingGallery
{
    public static void ThemingHowDarkModeWorks()
    {
        #region docsnippet:theming-how-dark-mode-works
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingHowDarkModeWorks2()
    {
        #region docsnippet:theming-how-dark-mode-works-2
        var theme = new IkonTheme
        {
            Mode = ThemeMode.Fixed,
            ["background"] = "#14100b",
            ["foreground"] = "#e8dcc4",
            // ...
        };
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingWarmBedtimeCozyLowStakesEveningReading()
    {
        #region docsnippet:theming-warm-bedtime-cozy-low-stakes-evening-reading
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingCyberpunkNeonHackerTerminalHighContrastGlow()
    {
        #region docsnippet:theming-cyberpunk-neon-hacker-terminal-high-contrast-glow
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingEditorialVintagePaperAndInkSerifGenerousMargins()
    {
        #region docsnippet:theming-editorial-vintage-paper-and-ink-serif-generous-margins
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingBrutalistHighContrastSharpCornersMonoType()
    {
        #region docsnippet:theming-brutalist-high-contrast-sharp-corners-mono-type
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingGlassmorphismSoftTranslucentLightPastels()
    {
        #region docsnippet:theming-glassmorphism-soft-translucent-light-pastels
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
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingPastelSoftFriendlyKidsWellness()
    {
        #region docsnippet:theming-pastel-soft-friendly-kids-wellness
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingNoirContrastDarkCinematicSingleAccent()
    {
        #region docsnippet:theming-noir-contrast-dark-cinematic-single-accent
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
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingSolarpunkNaturalOptimisticEarthy()
    {
        #region docsnippet:theming-solarpunk-natural-optimistic-earthy
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
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingCleanSaasNeutralProfessionalDefaultIsh()
    {
        #region docsnippet:theming-clean-saas-neutral-professional-default-ish
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

        Log.Instance.Debug($"{theme}");
    }

    public static void ThemingDarkProModernDarkNeutralProductivity()
    {
        #region docsnippet:theming-dark-pro-modern-dark-neutral-productivity
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
        #endregion

        Log.Instance.Debug($"{theme}");
    }
}
