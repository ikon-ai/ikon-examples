using Microsoft.EntityFrameworkCore;

namespace Ikon.App.Patterns.Examples;

#region example:style-organization
internal static class Styles
{
    public static readonly string[] PageContainer = [Container.Xl2, "py-8 px-4 min-h-screen"];
    public static readonly string[] MainCard = [Card.Default, Layout.Column.Lg, "p-10 w-full"];
}
#endregion

// The theme lives on the app's own UI accessor, so this holder is the app class the section is
// describing.
file sealed class ThemeExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:theme-customization
    private UI UI { get; } = new(app, new IkonTheme
    {
        // Brand cluster — every brand-tinted CSS var, set explicitly.
        ["primary"]              = "violet-500",
        ["bg-brand-solid"]       = "violet-500",
        ["bg-brand-solid-hover"] = "violet-600",
        ["text-brand"]           = "violet-500",
        ["border-brand"]         = "violet-500",
        ["primary-foreground"]   = "#ffffff",

        // Surfaces.
        ["background"]   = "slate-950",
        ["text-primary"] = "slate-50",
        ["card"]         = "slate-900",
        ["border-primary"] = "slate-700",

        // Type + shape.
        ["font-heading"] = "Inter",
        ["radius-base"]  = "rounded-lg",

        // Per-token Tailwind overrides (optional).
        ["amber-400"]  = "#F5A524",     // re-skin a Tailwind palette step app-wide
        ["rounded-lg"] = "1.25rem",     // tune one radius rung
        ["--hero-glow"] = "radial-gradient(circle, #F5A52488, transparent 70%)", // bespoke decorative ("--" declares a custom variable on purpose)

        DarkMode = new IkonTheme { ["primary"] = "violet-400", ["background"] = "slate-950" },
    });
    #endregion

    public void Use() => Log.Instance.Debug($"{UI}");
}

file static class MotionExamples
{
    public static void Basics(UIView view)
    {
        #region example:motion-basics
        // Fade in
        view.Box(["motion-[0:opacity-0,100:opacity-100] motion-duration-500ms"], content: view =>
        {
            view.Text([Text.Body], "I fade in!");
        });

        // Slide up + fade in
        view.Box(["motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"]);

        // Glow pulse (looping)
        view.Box(["motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"]);

        // Per-letter wave animation (each letter starts 100ms after the previous one)
        view.Text(["wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] wave:motion-duration-2500ms wave:motion-per-letter wave:motion-letter-delay-100ms wave:motion-loop"], "Hello");

        // Per-letter fade-in with stagger delay (letters appear one by one)
        view.Text(["motion-[0:opacity-0,100:opacity-100] motion-duration-300ms motion-per-letter motion-letter-delay-60ms"], "Appearing!");

        // Per-word animation
        view.Text(["motion-[0:opacity-0_translate-y-[10px],100:opacity-100_translate-y-0] motion-duration-500ms motion-per-word motion-letter-delay-100ms"], "Each word slides in");
        #endregion
    }

    public static void Advanced(UIView view)
    {
        #region example:motion-advanced
        // Shimmer/loading effect — translate a gradient overlay
        view.Box(["w-full h-4 rounded bg-muted relative overflow-hidden " +
            "before:content-[''] before:absolute before:inset-0 " +
            "before:bg-[linear-gradient(90deg,transparent,rgba(255,255,255,0.5),transparent)] " +
            "before:w-[200%] " +
            "before:shimmer:motion-[0:translate-x-[-50%],100:translate-x-[0%]] " +
            "before:shimmer:motion-duration-1000ms before:shimmer:motion-ease-linear before:shimmer:motion-loop"]);

        // Scale + blur entrance
        view.Box(["motion-[0:opacity-0_scale-[0.5]_blur-[4px],100:opacity-100_scale-100_blur-0] motion-duration-500ms"]);
        #endregion
        PatternDemoNote.RenderCaption(view, "The shimmer bar above is the loading effect; the scale-and-blur entrance sits on a box the example leaves empty, so it has nothing to show here");
    }
}

file sealed class MotionAppExamples(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region example:motion-main
    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column(["h-screen items-center justify-center gap-4"], content: view =>
            {
                // Animated heading with fade-in + slide
                view.Text([Text.H2, "motion-[0:opacity-0_translate-y-[20px],100:opacity-100_translate-y-0] motion-duration-700ms"], "Welcome!");

                // Pulsing glow button
                view.Button([Button.PrimaryMd, "motion-[0:shadow-none,50:shadow-[0_0_20px_rgba(168,85,247,0.6)],100:shadow-none] motion-duration-2000ms motion-loop"],
                    text: "Click me");
            });
        });
    }
    #endregion
}
