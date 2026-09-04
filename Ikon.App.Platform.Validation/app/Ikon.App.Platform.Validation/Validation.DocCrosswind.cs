// Generated holder for crosswind-styling-and-motion-guide.md — one class per guide SECTION, because a section is one
// reader's file: two of them may each declare a `UI` or a `Main` without either being wrong.
// Each class carries only the placeholder names its own fences use and do not declare.

file sealed class DocCwUsageInApplications(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-usage-in-applications-2
    private const string BaseButton = "px-4 py-2 rounded-lg font-medium transition-colors";
    private const string PrimaryColors = "bg-blue-500 hover:bg-blue-600 text-white";
    #endregion

    public async Task CwUsageInApplications(UIView view)
    {
        #region docsnippet:cw-usage-in-applications
        view.Button(
            style: ["px-4 py-2 rounded-lg bg-blue-500 hover:bg-blue-600 text-white transition"],
            text: "Click me",
            onClick: async () => { }
        );
        #endregion
    }

    public async Task CwUsageInApplications3(UIView view)
    {
        #region docsnippet:cw-usage-in-applications-3
        view.Button(
            style: [BaseButton, PrimaryColors],
            text: "Primary Action",
            onClick: async () => { }
        );
        #endregion
    }
}

file sealed class DocCwOrganizingStyles(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-organizing-styles
    public static class AppStyles
    {
        public static class Button
        {
            public const string Primary =
                "px-4 py-2 rounded-lg font-medium " +
                "bg-blue-500 hover:bg-blue-600 active:bg-blue-700 " +
                "text-white transition-colors";

            public const string Secondary =
                "px-4 py-2 rounded-lg font-medium " +
                "bg-neutral-700 hover:bg-neutral-600 " +
                "text-white transition-colors";
        }

        public static class Card
        {
            public const string Default =
                "p-4 rounded-xl " +
                "bg-neutral-900 border border-neutral-800";

            public const string Interactive =
                Default + " " +
                "hover:border-neutral-700 transition-colors cursor-pointer";
        }
    }
    #endregion

}

file sealed class DocCwThreeStylingLayersAllValidInTheSameStyleArray(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwThreeStylingLayersAllValidInTheSameStyleArray(UIView view)
    {
        #region docsnippet:cw-three-styling-layers-all-valid-in-the-same-style-array
        view.Box(style: ["rounded-2xl bg-card border border-secondary p-6 text-foreground"], content: view => { });
        #endregion
    }

    public async Task CwThreeStylingLayersAllValidInTheSameStyleArray2(UIView view)
    {
        #region docsnippet:cw-three-styling-layers-all-valid-in-the-same-style-array-2
        view.Button(style: [Button.PrimaryMd], text: "Submit");
        view.TextField(style: [Input.Default], defaultValue: "");
        view.Box(style: [Card.Default], content: view => { });
        #endregion
    }

    public async Task CwThreeStylingLayersAllValidInTheSameStyleArray3(UIView view)
    {
        #region docsnippet:cw-three-styling-layers-all-valid-in-the-same-style-array-3
        view.Button(style: ["px-4 py-2 bg-amber-400 text-zinc-950 rounded-md hover:bg-amber-500 transition-colors"], text: "Submit");
        view.Box(style: ["rounded-2xl bg-zinc-900 border border-zinc-800 p-6 shadow-lg"], content: view => { });
        #endregion
    }
}

file sealed class DocCwHoverOnlyAppliesOnDevicesThatCanHover(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwHoverOnlyAppliesOnDevicesThatCanHover(UIView view)
    {
        #region docsnippet:cw-hover-only-applies-on-devices-that-can-hover
        // Nav arrows: hover-revealed on desktop, always visible on touch
        view.Box(style: ["opacity-0 group-hover:opacity-100 pointer-coarse:opacity-100 transition-opacity"], content: view => { });
        #endregion
    }
}

file sealed class DocCwThemeActivation(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());

    #region docsnippet:cw-theme-activation
    private ThemeControl _theme = null!;

    public async Task Main()
    {
        _theme = UI.UseTheme(); // defaults: dark, and follows a client's own saved theme
    }
    #endregion

    public async Task CwThemeActivation2(UIView view)
    {
        #region docsnippet:cw-theme-activation-2
        view.Button([Button.GhostMd, Button.Icon],
            onClick: _theme.ToggleAsync,
            content: v => v.Icon([Icon.Default], name: _theme.Current.Value == Theme.Dark ? "sun" : "moon"));
        #endregion
    }
}

file sealed class DocCwVariants(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwVariants(UIView view)
    {
        #region docsnippet:cw-variants
        string[] Classes =
        [
            // Pseudo-classes
            "hover:bg-blue-600 focus:ring-2 active:scale-95 disabled:opacity-50",

            // Responsive breakpoints
            "sm:flex md:grid lg:hidden",

            // Dark mode
            "dark:bg-neutral-900 dark:text-white",

            // Data attributes
            "data-[state=open]:bg-blue-500 data-[disabled]:opacity-50",

            // Group and peer
            "group-hover:visible peer-focus:ring-2",
        ];
        #endregion
    }
}

file sealed class DocCwVariantGroups(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwVariantGroups(UIView view)
    {
        #region docsnippet:cw-variant-groups
        string[] Classes =
        [
            // These two are equivalent:
            "hover:bg-blue-600 hover:text-white hover:shadow-lg",
            "hover:(bg-blue-600 text-white shadow-lg)",
        ];
        #endregion
    }

    public async Task CwVariantGroups2(UIView view)
    {
        #region docsnippet:cw-variant-groups-2
        string[] Classes =
        [
            "md:(flex gap-4 hover:bg-blue-600)",   // md: applies to all three; hover: also to the last
        ];
        #endregion
    }
}

file sealed class DocCwTargetVariantsWebVsFlutter(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwTargetVariantsWebVsFlutter(UIView view)
    {
        #region docsnippet:cw-target-variants-web-vs-flutter
        // Shared layout, per-target colours. Use the group form so the target prefix is written once:
        string[] Classes =
        [
            "px-3 py-2 rounded-md",
            "web:(bg-background text-secondary border border-input)",
            "flutter:(bg-slate-900 text-slate-100 border border-slate-700)",
        ];
        #endregion
    }
}

file sealed class DocCwArbitraryValues(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwArbitraryValues(UIView view)
    {
        #region docsnippet:cw-arbitrary-values
        string[] Classes =
        [
            // Custom colors
            "bg-[#ff6b6b] text-[rgb(255,255,255)]",

            // Custom spacing
            "p-[13px] gap-[0.875rem]",

            // Custom properties
            "shadow-[0_0_20px_rgba(0,255,65,0.3)]",
        ];
        #endregion
    }
}

file sealed class DocCwKeyframeTimelines(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwKeyframeTimelines(UIView view)
    {
        #region docsnippet:cw-keyframe-timelines
        string[] Classes =
        [
            // Fade in and slide up
            "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0]",

            // Scale pulse
            "motion-[0:scale-100,50:scale-[1.05],100:scale-100]",

            // Complex multi-step animation
            "motion-[0:opacity-0_blur-[4px],30:opacity-60_blur-[2px],100:opacity-100_blur-0]",
        ];
        #endregion
    }
}

file sealed class DocCwTimingControls(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwTimingControls(UIView view)
    {
        #region docsnippet:cw-timing-controls
        string[] Classes =
        [
            // Duration and delay
            "motion-duration-300ms motion-delay-100ms",

            // Easing
            "motion-ease-[cubic-bezier(0.25,1,0.35,1)]",

            // Fill mode
            "motion-fill-both motion-fill-forwards",

            // Iteration
            "motion-once motion-loop motion-ping-pong",

            // Step easing (discrete/glitch effects)
            "motion-ease-[steps(1)]",   // instant jumps between keyframes
            "motion-ease-[steps(4)]",   // four evenly-spaced steps

            // Playback rate multiplier
            "motion-rate-150",           // 150% speed
        ];
        #endregion
    }
}

file sealed class DocCwStaggeredTextAnimations(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwStaggeredTextAnimations(UIView view)
    {
        #region docsnippet:cw-staggered-text-animations
        string[] Classes =
        [
            // Typewriter effect - letters appear one at a time
            "motion-[0:opacity-0,100:opacity-100] " +
            "motion-duration-80ms motion-stagger-50ms motion-per-letter motion-fill-both",

            // Words fade in sequentially
            "motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] " +
            "motion-duration-200ms motion-stagger-120ms motion-per-word motion-fill-both",

            // Lines reveal one by one
            "motion-[0:opacity-0,100:opacity-100] " +
            "motion-duration-300ms motion-stagger-200ms motion-per-line motion-fill-both",
        ];
        #endregion
    }
}

file sealed class DocCwPerElementModesAndCompoundVariants(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwPerElementModesAndCompoundVariants(UIView view)
    {
        #region docsnippet:cw-per-element-modes-and-compound-variants
        string[] Classes =
        [
            // Looping wave — each letter bounces continuously
            "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] " +
            "wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-loop wave:motion-ease-ease-in-out",

            // Reverse loop — stagger starts from the last letter
            "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] " +
            "wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-reverse-loop wave:motion-ease-ease-in-out",

            // Ping-pong — alternating direction per word
            "motion-[0:opacity-70_scale-[0.95],100:opacity-100_scale-100] " +
            "motion-duration-500ms motion-stagger-150ms motion-per-word-ping-pong",
        ];
        #endregion
    }
}

file sealed class DocCwTrackPrefixes(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwTrackPrefixes(UIView view)
    {
        #region docsnippet:cw-track-prefixes
        string[] Classes =
        [
            // 'title' track for text, 'glow' track for background effect
            "title:motion-[0:opacity-0,100:opacity-100] title:motion-duration-300ms title:motion-per-letter " +
            "glow:motion-[0:scale-100,50:scale-[1.02],100:scale-100] glow:motion-duration-2000ms glow:motion-loop",
        ];
        #endregion
    }
}

file sealed class DocCwStateBasedAnimations(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwStateBasedAnimations(UIView view)
    {
        #region docsnippet:cw-state-based-animations
        string[] Classes =
        [
            // Dialog content animation
            "data-[state=open]:motion-[0:opacity-0_scale-[0.95],100:opacity-100_scale-100] " +
            "data-[state=open]:motion-duration-200ms data-[state=open]:motion-fill-both " +
            "data-[state=closed]:motion-[0:opacity-100,100:opacity-0] " +
            "data-[state=closed]:motion-duration-150ms data-[state=closed]:motion-fill-both",
        ];
        #endregion
    }
}

file sealed class DocCw3dTransformsInKeyframes(IApp<SessionIdentity, ClientParams> app)
{

    public async Task Cw3dTransformsInKeyframes(UIView view)
    {
        #region docsnippet:cw-3d-transforms-in-keyframes
        string[] Classes =
        [
            // Card flip (Y-axis rotation)
            "motion-[0:rotate-y-0,50:rotate-y-[180deg],100:rotate-y-[360deg]] " +
            "motion-duration-3000ms motion-loop motion-ease-ease-in-out",

            // Depth pop with translate-z
            "motion-[0:translate-z-[-50px]_blur-[3px]_opacity-50_scale-[0.95]," +
            "50:translate-z-[10px]_blur-0_opacity-100_scale-[1.02]," +
            "100:translate-z-0_blur-0_opacity-100_scale-100] " +
            "motion-duration-600ms motion-stagger-40ms motion-per-letter-loop motion-ease-ease-out",

            // Cube face rotation (combined X + Y)
            "motion-[0:rotate-x-0_rotate-y-0," +
            "25:rotate-x-[90deg]_rotate-y-0," +
            "50:rotate-x-[90deg]_rotate-y-[90deg]," +
            "75:rotate-x-0_rotate-y-[90deg]," +
            "100:rotate-x-0_rotate-y-0] " +
            "motion-duration-4000ms motion-loop motion-ease-ease-in-out",
        ];
        #endregion
    }
}

file sealed class DocCwFilterAnimationsInKeyframes(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwFilterAnimationsInKeyframes(UIView view)
    {
        #region docsnippet:cw-filter-animations-in-keyframes
        string[] Classes =
        [
            // Hue rotation cycle — rainbow color shifting
            "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]] " +
            "motion-duration-3000ms motion-loop motion-ease-linear",

            // Brightness flash
            "motion-[0:brightness-100,15:brightness-[2],30:brightness-100,100:brightness-100] " +
            "motion-duration-2000ms motion-loop",

            // Saturate pulse
            "motion-[0:saturate-100,50:saturate-[2],100:saturate-100] " +
            "motion-duration-1500ms motion-loop motion-ease-ease-in-out",

            // Grayscale fade
            "motion-[0:grayscale-0,50:grayscale-100,100:grayscale-0] " +
            "motion-duration-4000ms motion-loop motion-ease-ease-in-out",

            // Combined filters (blur + brightness + hue-rotate)
            "motion-[0:blur-0_brightness-100_hue-rotate-0," +
            "25:blur-[2px]_brightness-[1.2]_hue-rotate-[45deg]," +
            "50:blur-[4px]_brightness-[1.5]_hue-rotate-[90deg]," +
            "75:blur-[2px]_brightness-[1.2]_hue-rotate-[135deg]," +
            "100:blur-0_brightness-100_hue-rotate-[180deg]] " +
            "motion-duration-4000ms motion-loop motion-ease-ease-in-out",
        ];
        #endregion
    }
}

file sealed class DocCwTextShadowAnimationsInKeyframes(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwTextShadowAnimationsInKeyframes(UIView view)
    {
        #region docsnippet:cw-text-shadow-animations-in-keyframes
        string[] Classes =
        [
            // Chromatic aberration glitch
            "glitch:motion-[0:text-shadow-[0_0_0_transparent,0_0_0_transparent]," +
            "20:text-shadow-[3px_0_0_rgba(255,0,0,0.8),-3px_0_0_rgba(0,255,255,0.8)]," +
            "40:text-shadow-[-2px_1px_0_rgba(255,0,0,0.6),2px_-1px_0_rgba(0,255,255,0.6)]," +
            "60:text-shadow-[2px_0_0_rgba(255,0,0,0.8),-2px_0_0_rgba(0,255,255,0.8)]," +
            "80:text-shadow-[-1px_-1px_0_rgba(255,0,0,0.5),1px_1px_0_rgba(0,255,255,0.5)]," +
            "100:text-shadow-[0_0_0_transparent,0_0_0_transparent]] " +
            "glitch:motion-duration-150ms glitch:motion-loop glitch:motion-ease-[steps(1)]",

            // Neon glow pulse
            "glow:motion-[0:text-shadow-[0_0_0_rgba(0,0,0,0)]," +
            "25:text-shadow-[0_0_0.5em_rgba(255,0,128,0.5)]," +
            "50:text-shadow-[0_0_0.8em_rgba(255,100,150,0.4)]," +
            "75:text-shadow-[0.1em_0_0_rgba(255,0,100,0.6),-0.1em_0_0_rgba(255,150,200,0.6)]," +
            "100:text-shadow-[0_0_0_rgba(0,0,0,0)]] " +
            "glow:motion-duration-3000ms glow:motion-loop",
        ];
        #endregion
    }
}

file sealed class DocCwCommonAnimationPatterns2(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-common-animation-patterns-2
    public static class Hover
    {
        // CSS transition - smoother for hover
        public const string Lift =
            "hover:-translate-y-[2px] hover:shadow-lg transition-all duration-200";

        // Motion-based hover (for complex sequences)
        public const string Glitch =
            "hover:motion-[0:translate-x-0,30:translate-x-[2px],60:translate-x-[-1px],100:translate-x-0] " +
            "hover:motion-duration-200ms";
    }
    #endregion
}

file sealed class DocCwCommonAnimationPatterns3(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-common-animation-patterns-3
    public static class Loop
    {
        public const string Pulse =
            "motion-[0:opacity-70,50:opacity-100,100:opacity-70] " +
            "motion-duration-2000ms motion-loop motion-ease-ease-in-out";

        public const string Breathe =
            "motion-[0:scale-100,50:scale-[1.02],100:scale-100] " +
            "motion-duration-3000ms motion-loop motion-ease-ease-in-out";
    }
    #endregion
}

file sealed class DocCwCommonAnimationPatterns(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-common-animation-patterns
    public static class Enter
    {
        public const string FadeUp =
            "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0] " +
            "motion-duration-300ms motion-ease-[cubic-bezier(0.25,1,0.35,1)] motion-fill-both";

        public const string ScaleIn =
            "motion-[0:opacity-0_scale-[0.95],100:opacity-100_scale-100] " +
            "motion-duration-300ms motion-ease-[cubic-bezier(0.25,1,0.35,1)] motion-fill-both";
    }
    #endregion

}

file sealed class DocCwCaveatBareOutlineLeavesOutlineStyleNone(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwCaveatBareOutlineLeavesOutlineStyleNone(UIView view)
    {
        #region docsnippet:cw-caveat-bare-outline-leaves-outline-style-none
        // WRONG — outline-style stays `none`, nothing renders
        view.Box(style: ["outline outline-2 outline-blue-500"]);

        // CORRECT — explicit style
        view.Box(style: ["outline outline-solid outline-2 outline-blue-500"]);
        #endregion
    }
}

file sealed class DocCwScrollTimelines(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwScrollTimelines(UIView view)
    {
        #region docsnippet:cw-scroll-timelines
        string[] Classes =
        [
            // On the scroll container
            "scroll-timeline-[--hero_y]",

            // On the animated element
            "lead:motion-[0:opacity-0,100:opacity-100] lead:motion-timeline-[--hero]",
        ];
        #endregion
    }
}

file sealed class DocCwAnimationComposition(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwAnimationComposition(UIView view)
    {
        #region docsnippet:cw-animation-composition
        string[] Classes =
        [
            // Additive composition — transforms blend instead of replacing
            "pulse:motion-[0:scale-100,100:scale-110] pulse:motion-composition-add",
        ];
        #endregion
    }
}

file sealed class DocCwPlayStateControl(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwPlayStateControl(UIView view)
    {
        #region docsnippet:cw-play-state-control
        string[] Classes =
        [
            "lead:motion-play-state-paused",    // starts paused
            "lead:motion-play-state-running",   // resumes
        ];
        #endregion
    }
}

file sealed class DocCwAnimationRange(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwAnimationRange(UIView view)
    {
        #region docsnippet:cw-animation-range
        string[] Classes =
        [
            "halo:motion-range-[entry_0%_exit_60%]",
            "halo:motion-range-start-[entry_10%]",
            "halo:motion-range-end-[exit_90%]",
        ];
        #endregion
    }
}

file sealed class DocCwMotionPriority(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwMotionPriority(UIView view)
    {
        #region docsnippet:cw-motion-priority
        string[] Classes =
        [
            "motion-priority-0",     // default
            "motion-priority-100",   // higher priority staggers first
        ];
        #endregion
    }
}

file sealed class DocCwGpuPromotionWillChange(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwGpuPromotionWillChange(UIView view)
    {
        #region docsnippet:cw-gpu-promotion-will-change
        string[] Classes =
        [
            // Force promotion even for one-shot animations (e.g. a critical entry effect)
            "intro:motion-[0:opacity-0,100:opacity-100] intro:motion-once intro:motion-promote",

            // Suppress promotion for continuous animations on plentiful elements
            // (e.g. a looping pulse on hundreds of list items where GPU-layer cost dominates)
            "pulse:motion-[0:opacity-70,100:opacity-100] pulse:motion-loop pulse:motion-no-promote",
        ];
        #endregion
    }
}

file sealed class DocCwCompleteExample(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:cw-complete-example
    public static class Button
    {
        private const string Base =
            "px-4 py-2 rounded-lg font-medium " +
            "transition-all duration-200";

        private const string PrimaryColors =
            "bg-blue-500 hover:bg-blue-600 active:bg-blue-700 " +
            "text-white border border-blue-400/50";

        private const string HoverEffect =
            "hover:-translate-y-[1px] hover:shadow-lg";

        private const string ActivePress =
            "active:motion-[0:scale-100,50:scale-[0.97],100:scale-100] " +
            "active:motion-duration-150ms";

        public const string Primary = Base + " " + PrimaryColors + " " + HoverEffect + " " + ActivePress;
    }
    #endregion

    public async Task CwCompleteExample2(UIView view)
    {
        #region docsnippet:cw-complete-example-2
        view.Button(style: [Button.Primary], text: "Submit", onClick: async () => { });
        #endregion
    }
}

file sealed class DocCwFullScreenLayoutsWithPadding(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new IkonTheme());


    public async Task CwFullScreenLayoutsWithPadding(UIView view)
    {
        #region docsnippet:cw-full-screen-layouts-with-padding
        // Option 1: Padding on Root (preferred for semantic clarity)
        UI.Root(style: ["h-screen bg-slate-950 p-4"], content: view =>
        {
            view.Column(style: ["w-full h-full"], content: col => { });
        });

        // Option 2: Padding on inner container (also works)
        UI.Root(style: ["h-screen bg-slate-950"], content: view =>
        {
            view.Column(style: ["w-full h-full p-4"], content: col => { });
        });
        #endregion
    }
}

file sealed class DocCwWidthAndSizingContext(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwWidthAndSizingContext(UIView view)
    {
        #region docsnippet:cw-width-and-sizing-context
        // WRONG — percentage on child of auto-width absolute element
        view.Box(["absolute"], content: view =>
        {
            view.Column(["w-1/4"], content: v => { }); // Collapses to zero!
        });

        // RIGHT — percentage on child of flex container
        view.Row(["flex-1 min-w-0"], content: view =>
        {
            view.Column(["w-1/4 flex-shrink-0"], content: v => { }); // 25% of parent flex item
            view.Column(["flex-1 min-w-0"], content: v => { });      // Remaining space
        });
        #endregion
    }
}

file sealed class DocCwIconSizing(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView row = null!;
    private void UseRow() => Log.Instance.Debug($"{row}");


    public async Task CwIconSizing(UIView view)
    {
        #region docsnippet:cw-icon-sizing
        row.Icon(style: ["w-4 h-4"], name: "message-circle");  // 16x16
        row.Icon(style: ["w-5 h-5"], name: "settings");        // 20x20
        #endregion
    }
}

file sealed class DocCwCanonicalIconTextPattern(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView row = null!;
    private void UseRow() => Log.Instance.Debug($"{row}");


    public async Task CwCanonicalIconTextPattern(UIView view)
    {
        #region docsnippet:cw-canonical-icon-text-pattern
        // Button with icon and text
        view.Button(
            style: ["text-white bg-blue-600 px-4 py-2 rounded-lg"],  // Always set text color!
            content: btn =>
            {
                btn.Row(style: ["flex items-center gap-3"], content: row =>
                {
                    row.Icon(style: ["w-4 h-4"], name: "play");
                    row.Text(text: "Activate");
                });
            });
        #endregion
    }
}

file sealed class DocCwCommonMistakes(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwCommonMistakes(UIView view)
    {
        #region docsnippet:cw-common-mistakes
        // WRONG: Missing text color = black/invisible icons
        view.Icon(style: ["bg-blue-600"], name: "check");

        // CORRECT: Always include text color
        view.Icon(style: ["text-white bg-blue-600"], name: "check");
        #endregion
    }
}

file sealed class DocCwFullscreenEffectsAndOverflow(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwFullscreenEffectsAndOverflow(UIView view)
    {
        #region docsnippet:cw-fullscreen-effects-and-overflow
        // WRONG: sweep band moves outside bounds, creates scrollbar
        view.Column(style: ["absolute inset-0 pointer-events-none"], content: overlay =>
        {
            overlay.Box(style: ["absolute w-full h-[2px] bg-white/10 " +
                "motion-[0:translate-y-[-100px],100:translate-y-[900px]] motion-duration-4000ms motion-loop"]);
        });

        // CORRECT: overflow-hidden prevents scrollbar
        view.Column(style: ["absolute inset-0 pointer-events-none overflow-hidden"], content: overlay =>
        {
            overlay.Box(style: ["absolute w-full h-[2px] bg-white/10 " +
                "motion-[0:translate-y-[-100px],100:translate-y-[900px]] motion-duration-4000ms motion-loop"]);
        });
        #endregion
    }
}

file sealed class DocCwResponsiveBreakpoints(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView row = null!;
    private void UseRow() => Log.Instance.Debug($"{row}");


    public async Task CwResponsiveBreakpoints(UIView view)
    {
        #region docsnippet:cw-responsive-breakpoints
        // Padding: 12px on mobile, 16px on sm+, 24px on md+
        view.Column(style: ["p-3 sm:p-4 md:p-6"], content: col => { });

        // Hidden on mobile, visible on sm+
        row.Text(text: "Projects", style: ["hidden sm:block"]);

        // Different layouts per breakpoint
        view.Column(style: ["flex flex-col sm:flex-row"], content: col => { });

        // Sidebar: overlay on mobile, inline on desktop
        view.Box(style: ["absolute inset-y-0 left-0 z-50 md:static md:z-auto"], content: v => { });

        // Backdrop: visible on mobile only
        view.Box(style: ["absolute inset-0 bg-black/50 z-40 md:hidden"], content: v => { });
        #endregion
    }
}

file sealed class DocCwVisualHierarchyWithGradients(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwVisualHierarchyWithGradients(UIView view)
    {
        #region docsnippet:cw-visual-hierarchy-with-gradients
        // Primary button - gradient with shadow
        view.Button(
            style: ["px-4 py-2 text-sm bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 rounded-xl font-medium transition-all shadow-lg"],
            text: "Primary Action",
            onClick: async () => { });

        // Secondary button - subtle with border
        view.Button(
            style: ["px-4 py-2 text-sm bg-slate-700/50 hover:bg-slate-600/50 text-slate-300 hover:text-white rounded-xl font-medium transition-all border border-slate-600/50"],
            text: "Secondary",
            onClick: async () => { });
        #endregion
    }
}

file sealed class DocCwIconContainersWithGradientBackgrounds(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView container = null!;
    private void UseContainer() => Log.Instance.Debug($"{container}");


    public async Task CwIconContainersWithGradientBackgrounds(UIView view)
    {
        #region docsnippet:cw-icon-containers-with-gradient-backgrounds
        container.Column(style: ["w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500/20 to-purple-500/20 flex items-center justify-center flex-shrink-0 border border-slate-600/30"], content: iconWrap =>
        {
            iconWrap.Icon(style: ["w-4 h-4 text-blue-400"], name: "wrench");
        });
        #endregion
    }
}

file sealed class DocCwCardsAndContainers(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView list = null!;
    private void UseList() => Log.Instance.Debug($"{list}");
    private readonly UIView details = null!;
    private void UseDetails() => Log.Instance.Debug($"{details}");
    private readonly bool isSelected = false;
    private void UseIsSelected() => Log.Instance.Debug($"{isSelected}");


    public async Task CwCardsAndContainers(UIView view)
    {
        #region docsnippet:cw-cards-and-containers
        // Card with hover state
        list.Column(style: [
            "bg-slate-800/50 rounded-2xl p-4 cursor-pointer transition-all border",
            isSelected ? "border-blue-500/50 bg-slate-800/80" : "border-slate-700/50 hover:bg-slate-700/50 hover:border-slate-600/50"
        ], content: card => { });

        // Nested content container
        details.Row(style: ["flex items-start gap-3 bg-slate-900/50 p-3 rounded-xl border border-slate-700/30"], content: row => { });
        #endregion
    }
}

file sealed class DocCwBadgesAndPills(IApp<SessionIdentity, ClientParams> app)
{
    private readonly UIView container = null!;
    private void UseContainer() => Log.Instance.Debug($"{container}");
    private readonly UIView list = null!;
    private void UseList() => Log.Instance.Debug($"{list}");
    private readonly UIView titleRow = null!;
    private void UseTitleRow() => Log.Instance.Debug($"{titleRow}");


    public async Task CwBadgesAndPills(UIView view)
    {
        #region docsnippet:cw-badges-and-pills
        // Status badge with gradient
        titleRow.Text(text: "● ACTIVE", style: ["text-xs px-2.5 py-1 rounded-full bg-gradient-to-r from-blue-600 to-purple-600 font-medium shadow-sm"]);

        // Tag badge - solid color
        titleRow.Text(text: "manual", style: ["text-xs px-2.5 py-1 rounded-full bg-slate-600 font-medium"]);

        // Trigger pills in a horizontal list
        container.Row(style: ["flex flex-wrap gap-2"], content: list =>
        {
            list.Row(style: ["flex items-center gap-2 text-xs text-slate-300 bg-slate-800/80 px-3 py-1.5 rounded-full border border-slate-600/50"], content: pill =>
            {
                pill.Icon(style: ["w-3 h-3 text-purple-400"], name: "hash");
                pill.Text(text: "keyword");
            });
        });
        #endregion
    }
}

file sealed class DocCwSpacingScale(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwSpacingScale(UIView view)
    {
        #region docsnippet:cw-spacing-scale
        string[] Classes =
        [
            // Container padding (responsive)
            "p-3 sm:p-4 md:p-6",

            // Gap between items
            "gap-2", // tight (8px)
            "gap-3", // comfortable (12px)
            "gap-4", // spacious (16px)

            // Vertical spacing in lists
            "space-y-2", // tight
            "space-y-3", // comfortable
            "space-y-4", // spacious
        ];
        #endregion
    }
}

file sealed class DocCwTransitionsAndInteractions(IApp<SessionIdentity, ClientParams> app)
{

    public async Task CwTransitionsAndInteractions(UIView view)
    {
        #region docsnippet:cw-transitions-and-interactions
        string[] Classes =
        [
            "transition-all duration-200", // for color and transform changes
            "hover:bg-slate-700/50",       // subtle hover background
            "hover:text-white",            // brighten text on hover
            "hover:border-slate-600/50",   // subtle border change
        ];
        #endregion
    }
}
