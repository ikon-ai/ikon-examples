// The theming guide's remaining fences.
//
// Two of them declare the app's own `UI` property, so each needs a class of its own; the three
// palette-override fences used to be single initializer LINES, which belong to no declaration and
// compile nowhere — each is the whole initializer now, which is what a reader copies anyway.

file sealed class DocThemeCommitted(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:theming-committed-palette
    private UI UI { get; } = new(app, new IkonTheme
    {
        // One committed palette, pinned (no OS dark flip). For an adaptive app,
        // drop Fixed and add a DarkMode block instead — see "How dark mode works".
        Mode = ThemeMode.Fixed,

        ["primary"]              = "amber-400",  // whole brand cluster: CTAs, checked controls, focus rings, brand icons + text
        ["primary-foreground"]   = "#0A0A0A",    // text on brand fills — needed because amber-400 is a light step

        ["background"]           = "zinc-950",
        ["foreground"]           = "amber-50",
        ["card"]                 = "zinc-900",
        ["muted-foreground"]     = "zinc-500",
        ["border"]               = "zinc-800",

        ["radius"]               = "rounded-2xl",
        ["density"]              = "comfortable",
        ["font-heading"]         = "Crimson Pro", // literal family name — the Google Fonts import is automatic

        ["motion-duration-base"] = "200ms",
        ["ease-default"]         = "ease-out",
    });
    #endregion

    public void Use() => Log.Instance.Debug($"{UI}");
}

file sealed class DocThemeWholeApp(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:theming-whole-app
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

file static class DocThemingOverrides
{
    public static void PaletteSteps()
    {
        #region docsnippet:theming-palette-steps
        var theme = new IkonTheme
        {
            ["amber-400"] = "#F5A524",
            ["zinc-950"]  = "#0a0a0f",
        };
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void RadiusRungs()
    {
        #region docsnippet:theming-radius-rungs
        var theme = new IkonTheme
        {
            ["rounded-lg"] = "1.25rem",      // tune one rung
            ["rounded-xl"] = "rounded-3xl",  // re-point one rung at another
        };
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void CustomVariable()
    {
        #region docsnippet:theming-custom-variable
        var theme = new IkonTheme
        {
            ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)",
        };
        #endregion

        Log.Instance.Debug($"{theme}");
    }

    public static void UsingTheTokens(UIView view)
    {
        #region docsnippet:theming-using-tokens
        // Brand button — follows ["primary"] and ["primary-foreground"].
        view.Button(["bg-brand-solid hover:bg-brand-solid-hover text-primary-on-brand px-6 py-3 rounded-lg font-semibold"],
            "Launch", onClick: async () => { });

        // Standard surfaces + text tiers.
        view.Box(["bg-card border border-secondary rounded-lg p-6"], content: view => { });
        view.Text(["text-foreground"], "Body copy");
        view.Text(["text-sm text-muted-foreground"], "Caption");

        // Brand-tinted heading.
        view.Text(["text-2xl font-bold text-brand-secondary"], "Section Title");

        // Custom variable (declared with a -- prefix in the theme).
        view.Box(["absolute inset-0 -z-10 bg-[var(--hero-glow)] pointer-events-none"]);
        #endregion
    }
}
