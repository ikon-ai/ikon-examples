namespace Ikon.App.Patterns.Patterns;

// Pattern: theme-commitment — see docs/patterns/theme-commitment.md.
// `app` is the App's primary-constructor handle in a real app; the UI property below is the exact
// artifact the pattern teaches — a committed `new IkonTheme { ... }` at the declaration site.
internal sealed class ThemeCommitment(IAppBase app) : IPatternDemo
{
    public string Slug => "theme-commitment";
    public string Title => "Theme commitment";
    public string Category => "Layout";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Configuration pattern with no standalone UI: commits the whole brand cluster — palette, radius, density and fonts — in one IkonTheme at the UI declaration site. See the source and docs/patterns/theme-commitment.md.");

    #region docsnippet:pattern-theme-commitment
    // Pasted at the top of your App class — the Coder's standard styling step.
    // ONE line commits the whole brand cluster: ["primary"] expands to the CTA and
    // solid fills (+ hovers), focus rings, brand borders, brand icons, and brand text tiers.
    private UI UI { get; } = new(app, new IkonTheme
    {
        ["primary"]            = "amber-400",
        ["primary-foreground"] = "#0A0A0A",   // text on brand fills — only needed for LIGHT brand steps (white default)

        ["background"]       = "zinc-950",
        ["foreground"]       = "zinc-50",
        ["card"]             = "zinc-900",
        ["muted-foreground"] = "zinc-500",
        ["border"]           = "zinc-800",

        ["radius"]           = "rounded-2xl",
        ["density"]          = "comfortable",   // compact | comfortable | airy — whole-app whitespace
        ["font-heading"]     = "Crimson Pro",   // literal family name — Google Fonts import is automatic

        DarkMode = new IkonTheme
        {
            ["primary"]    = "amber-300",
            ["background"] = "zinc-950",
            ["foreground"] = "zinc-50",
            ["card"]       = "zinc-900",
        },
    });
    #endregion
}
