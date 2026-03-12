public partial class Validation
{
    private void RenderCrosswindSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H1, "mb-2"], "Motion & Animation Effects");
            view.Text([Text.Caption, "mb-4"], "Crosswind CSS motion utilities demonstration");

            view.Tabs(
                value: _crosswindSubTab.Value,
                onValueChange: async value => _crosswindSubTab.Value = value ?? "retro",
                listContainerStyle: [Card.Default, "p-2 mb-4"],
                listStyle: [Tabs.List, "flex-wrap bg-transparent gap-1"],
                triggerStyle: [Tabs.Trigger, "text-xs px-2 py-1"],
                contentStyle: [Tabs.Content],
                tabs: [
                    new TabItem("retro", "Retro/CRT", RenderRetroEffects),
                    new TabItem("reveal", "Reveal", RenderRevealEffects),
                    new TabItem("image-filters", "Image Filters", RenderImageFilterEffects),
                    new TabItem("3d-transforms", "3D Transforms", Render3DTransformEffects),
                    new TabItem("borders", "Borders", RenderBorderEffects),
                    new TabItem("chromatic", "Chromatic", RenderChromaticAberrationEffects),
                    new TabItem("filters", "Filters", RenderFilterEffects),
                    new TabItem("shadows", "Shadows", RenderShadowEffects),
                    new TabItem("creative", "Creative", RenderCreativeEffects),
                    new TabItem("per-letter", "Per-Letter", RenderPerLetterAnimations),
                    new TabItem("per-word", "Per-Word", RenderPerWordAnimations),
                    new TabItem("per-line", "Per-Line", RenderPerLineAnimations),
                    new TabItem("looping", "Looping", RenderLoopingAnimations),
                    new TabItem("entry", "Entry", RenderEntryAnimations),
                    new TabItem("multi-track", "Multi-Track", RenderMultiTrackAnimations),
                    new TabItem("timing", "Timing", RenderTimingControls)
                ]);
        });
    }

    private void RenderPerLetterAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Per-Letter Animations");
            view.Text([Text.Caption, "mb-6"], "Text animations applied to each letter individually with staggered timing");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Wave animation
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Wave Effect");
                    view.Text([
                        Text.H3,
                        "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0]",
                        "wave:motion-duration-800ms wave:motion-stagger-80ms wave:motion-per-letter-loop wave:motion-fill-both"
                    ], "Hello World");
                });

                // Ping-pong wave
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Ping-Pong Wave");
                    view.Text([
                        Text.H3,
                        "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0]",
                        "wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-ping-pong wave:motion-ease-ease-in-out"
                    ], "Ping Pong");
                });

                // Looping wave
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Looping Wave");
                    view.Text([
                        Text.H3,
                        "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0]",
                        "wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-loop wave:motion-ease-ease-in-out"
                    ], "Loop Wave");
                });

                // Reverse looping wave
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Reverse Looping Wave");
                    view.Text([
                        Text.H3,
                        "wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0]",
                        "wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-reverse-loop wave:motion-ease-ease-in-out"
                    ], "Reverse Wave");
                });

                // One-shot typewriter (letters appear and stay visible)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Typewriter (One-Shot)");
                    view.Text([
                        Text.H3,
                        "motion-[0:opacity-0,100:opacity-100]",
                        "motion-duration-100ms motion-stagger-80ms motion-per-letter motion-fill-both"
                    ], "Typing text...");
                });

                // Letter pulse wave (looping - each letter fades in/out at different phases)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Letter Pulse Wave");
                    view.Text([
                        Text.H3,
                        "motion-[0:opacity-0,50:opacity-100,100:opacity-0]",
                        "motion-duration-2000ms motion-stagger-100ms motion-per-letter-loop"
                    ], "Wave pulse...");
                });

                // Scale and blur wave (looping - each letter scales in/out)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Scale & Blur Wave");
                    view.Text([
                        Text.H3,
                        "motion-[0:opacity-0_scale-[0.5]_blur-[4px],50:opacity-100_scale-100_blur-0,100:opacity-0_scale-[0.5]_blur-[4px]]",
                        "motion-duration-2000ms motion-stagger-60ms motion-per-letter-loop"
                    ], "Scale Blur");
                });

                // Slide up wave (looping - each letter slides up/down)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Slide Up Wave");
                    view.Text([
                        Text.H3,
                        "motion-[0:opacity-0_translate-y-[20px],50:opacity-100_translate-y-0,100:opacity-0_translate-y-[20px]]",
                        "motion-duration-2000ms motion-stagger-50ms motion-per-letter-loop"
                    ], "Slide Up Text");
                });

                // Rotate wave (looping - each letter rotates in/out)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Rotate Wave");
                    view.Text([
                        Text.H3,
                        "motion-[0:opacity-0_rotate-[-90deg],50:opacity-100_rotate-0,100:opacity-0_rotate-[-90deg]]",
                        "motion-duration-2500ms motion-stagger-80ms motion-per-letter-loop"
                    ], "ROTATE");
                });
            });
        });
    }

    private void RenderPerWordAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Per-Word Animations");
            view.Text([Text.Caption, "mb-6"], "Text animations applied to each word with staggered timing");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Word fade
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Fade Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_translate-y-[8px],50:opacity-100_translate-y-0,100:opacity-0_translate-y-[8px]]",
                        "motion-duration-2000ms motion-stagger-200ms motion-per-word-loop"
                    ], "Each word fades in and out");
                });

                // Word slide
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Slide Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_translate-x-[-20px],50:opacity-100_translate-x-0,100:opacity-0_translate-x-[-20px]]",
                        "motion-duration-2000ms motion-stagger-200ms motion-per-word-loop"
                    ], "Words slide in from left");
                });

                // Word bounce
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Bounce Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_translate-y-[30px],35:translate-y-[-5px],50:opacity-100_translate-y-0,65:translate-y-[-5px],100:opacity-0_translate-y-[30px]]",
                        "motion-duration-2500ms motion-stagger-250ms motion-per-word-loop"
                    ], "Bouncy word animation");
                });

                // Word scale
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Scale Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_scale-[0.8],50:opacity-100_scale-100,100:opacity-0_scale-[0.8]]",
                        "motion-duration-2000ms motion-stagger-150ms motion-per-word-loop"
                    ], "Words scale up and down");
                });

                // Word wave loop
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Wave Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:translate-y-0,50:translate-y-[-8px],100:translate-y-0]",
                        "motion-duration-600ms motion-stagger-200ms motion-per-word-loop motion-ease-ease-in-out"
                    ], "Words bounce up and down");
                });

                // Word ping-pong
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Ping-Pong");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-70_scale-[0.95],100:opacity-100_scale-100]",
                        "motion-duration-500ms motion-stagger-150ms motion-per-word-ping-pong"
                    ], "Subtle word pulsing");
                });

                // Word reverse loop
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Word Reverse Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:translate-x-0,50:translate-x-[5px],100:translate-x-0]",
                        "motion-duration-400ms motion-stagger-120ms motion-per-word-reverse-loop motion-ease-ease-in-out"
                    ], "Words slide from last to first");
                });
            });
        });
    }

    private void RenderPerLineAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Per-Line Animations");
            view.Text([Text.Caption, "mb-6"], "Text animations applied line by line");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Line fade
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Fade Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0,50:opacity-100,100:opacity-0]",
                        "motion-duration-2000ms motion-stagger-300ms motion-per-line-loop"
                    ], "First line fades\nSecond line follows\nThird line last");
                });

                // Line slide up
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Slide Up Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_translate-y-[20px],50:opacity-100_translate-y-0,100:opacity-0_translate-y-[20px]]",
                        "motion-duration-2000ms motion-stagger-350ms motion-per-line-loop"
                    ], "Lines slide up\nOne after another\nSmooth reveal");
                });

                // Line slide from left
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Slide Left Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-0_translate-x-[-30px],50:opacity-100_translate-x-0,100:opacity-0_translate-x-[-30px]]",
                        "motion-duration-2000ms motion-stagger-250ms motion-per-line-loop"
                    ], "Sliding from left\nEach line reveals\nWith a delay");
                });

                // Line wave loop
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Wave Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:translate-y-0,50:translate-y-[-6px],100:translate-y-0]",
                        "motion-duration-800ms motion-stagger-300ms motion-per-line-loop motion-ease-ease-in-out"
                    ], "First line waves\nSecond follows\nThird in sequence");
                });

                // Line ping-pong
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Ping-Pong");
                    view.Text([
                        Text.Body,
                        "motion-[0:opacity-80_translate-x-0,100:opacity-100_translate-x-[8px]]",
                        "motion-duration-600ms motion-stagger-200ms motion-per-line-ping-pong"
                    ], "Lines shift back\nAnd forth gently\nIn alternation");
                });

                // Line reverse loop
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Line Reverse Loop");
                    view.Text([
                        Text.Body,
                        "motion-[0:scale-100,50:scale-[1.02],100:scale-100]",
                        "motion-duration-700ms motion-stagger-250ms motion-per-line-reverse-loop motion-ease-ease-in-out"
                    ], "Last line first\nMiddle follows\nTop line last");
                });
            });
        });
    }

    private void RenderLoopingAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Looping Animations");
            view.Text([Text.Caption, "mb-6"], "Continuous animations that repeat indefinitely");

            view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
            {
                // Pulse
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-full bg-primary",
                        "motion-[0:opacity-70,50:opacity-100,100:opacity-70]",
                        "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                    ]);
                    view.Text([Text.Caption], "Pulse");
                });

                // Breathing
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-lg bg-secondary",
                        "motion-[0:scale-100,50:scale-[1.1],100:scale-100]",
                        "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                    ]);
                    view.Text([Text.Caption], "Breathe");
                });

                // Ping-pong rotate
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-md bg-accent",
                        "motion-[0:rotate-[-15deg],100:rotate-[15deg]]",
                        "motion-duration-1000ms motion-ping-pong motion-ease-ease-in-out"
                    ]);
                    view.Text([Text.Caption], "Ping-Pong");
                });

                // Float
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-full bg-success",
                        "motion-[0:translate-y-0,50:translate-y-[-8px],100:translate-y-0]",
                        "motion-duration-2500ms motion-loop motion-ease-ease-in-out"
                    ]);
                    view.Text([Text.Caption], "Float");
                });

                // Spin
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-md bg-warning",
                        "motion-[0:rotate-0,100:rotate-[360deg]]",
                        "motion-duration-3000ms motion-loop motion-ease-linear"
                    ]);
                    view.Text([Text.Caption], "Spin");
                });

                // Shimmer
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-16 h-16 rounded-lg bg-muted",
                        "motion-[0:opacity-40,50:opacity-80,100:opacity-40]",
                        "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                    ]);
                    view.Text([Text.Caption], "Shimmer");
                });
            });
        });
    }

    private void RenderEntryAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Entry Animations (Looping)");
            view.Text([Text.Caption, "mb-6"], "Entry-style animations set to loop for visibility testing");

            view.Row([Layout.Row.Lg, "flex-wrap"], content: view =>
            {
                // Fade up
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-primary flex items-center justify-center",
                        "motion-[0:opacity-0_translate-y-[12px],50:opacity-100_translate-y-0,100:opacity-0_translate-y-[12px]]",
                        "motion-duration-2000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-primary-foreground"], "Fade"));
                    view.Text([Text.Caption], "Fade Up");
                });

                // Scale in
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-secondary flex items-center justify-center",
                        "motion-[0:opacity-0_scale-[0.8],50:opacity-100_scale-100,100:opacity-0_scale-[0.8]]",
                        "motion-duration-2000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-secondary-foreground"], "Scale"));
                    view.Text([Text.Caption], "Scale In");
                });

                // Slide right
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-accent flex items-center justify-center",
                        "motion-[0:opacity-0_translate-x-[-20px],50:opacity-100_translate-x-0,100:opacity-0_translate-x-[-20px]]",
                        "motion-duration-2000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-accent-foreground"], "Slide"));
                    view.Text([Text.Caption], "Slide Right");
                });

                // Blur in
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-success flex items-center justify-center",
                        "motion-[0:opacity-0_blur-[8px],50:opacity-100_blur-0,100:opacity-0_blur-[8px]]",
                        "motion-duration-2000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-success-foreground"], "Blur"));
                    view.Text([Text.Caption], "Blur In");
                });

                // Bounce in
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-warning flex items-center justify-center",
                        "motion-[0:scale-[0.5],25:scale-[1.05],35:scale-[0.95],50:scale-100,75:scale-[0.95],85:scale-[1.05],100:scale-[0.5]]",
                        "motion-duration-3000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-warning-foreground"], "Bounce"));
                    view.Text([Text.Caption], "Bounce In");
                });

                // Flip in
                view.Column([Layout.Column.Sm, "items-center"], content: view =>
                {
                    view.Box([
                        "w-20 h-20 rounded-lg bg-destructive flex items-center justify-center",
                        "motion-[0:rotate-y-[-180deg],50:rotate-y-0,100:rotate-y-[-180deg]]",
                        "motion-duration-2000ms motion-loop"
                    ], content: v => v.Text([Text.Small, "text-destructive-foreground"], "Flip"));
                    view.Text([Text.Caption], "Flip In");
                });
            });
        });
    }

    private void RenderMultiTrackAnimations(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Multi-Track Animations");
            view.Text([Text.Caption, "mb-6"], "Multiple independent animation tracks on the same element");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Text with glow effect
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Text Track + Glow Track");
                    view.Text([
                        Text.H3,
                        "title:motion-[0:opacity-0,50:opacity-100,100:opacity-0] title:motion-duration-2000ms title:motion-per-letter title:motion-stagger-40ms title:motion-loop",
                        "glow:motion-[0:scale-100,50:scale-[1.02],100:scale-100] glow:motion-duration-2000ms glow:motion-loop glow:motion-ease-ease-in-out"
                    ], "Glowing Text");
                });

                // Spin + Pulse with independent tracks (uses CSS variable decomposition)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Spin + Pulse (Independent Tracks)");
                    view.Box([
                        "w-24 h-24 rounded-lg bg-gradient-to-br from-primary to-secondary",
                        "spin:motion-[0:rotate-0,100:rotate-[360deg]] spin:motion-duration-3000ms spin:motion-loop spin:motion-ease-linear",
                        "pulse:motion-[0:scale-100,50:scale-[1.15],100:scale-100] pulse:motion-duration-1000ms pulse:motion-loop pulse:motion-ease-ease-in-out"
                    ]);
                });

                // Spin + Translate with independent tracks
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Spin + Float (Independent Tracks)");
                    view.Box([
                        "w-24 h-24 rounded-md bg-gradient-to-br from-accent to-success",
                        "spin:motion-[0:rotate-0,100:rotate-[360deg]] spin:motion-duration-4000ms spin:motion-loop spin:motion-ease-linear",
                        "float:motion-[0:translate-y-0,50:translate-y-[-12px],100:translate-y-0] float:motion-duration-2000ms float:motion-loop float:motion-ease-ease-in-out"
                    ]);
                });

                // Combined fade and slide
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Fade Track + Slide Track");
                    view.Box([
                        "w-24 h-8 rounded bg-accent",
                        "fade:motion-[0:opacity-50,100:opacity-100] fade:motion-duration-1000ms fade:motion-ping-pong fade:motion-ease-ease-in-out",
                        "slide:motion-[0:translate-x-0,100:translate-x-[20px]] slide:motion-duration-2000ms slide:motion-ping-pong slide:motion-ease-ease-in-out"
                    ]);
                });
            });
        });
    }

    private void RenderTimingControls(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Timing Controls");
            view.Text([Text.Caption, "mb-6"], "Duration, easing, stagger, and delay demonstrations");

            view.Column([Layout.Column.Xl], content: view =>
            {
                // Duration comparison
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H3, "mb-3"], "Duration Comparison");
                    view.Row([Layout.Row.Lg, "flex-wrap items-end"], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-primary",
                                "motion-[0:translate-y-0,50:translate-y-[-20px],100:translate-y-0]",
                                "motion-duration-200ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "200ms");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-primary",
                                "motion-[0:translate-y-0,50:translate-y-[-20px],100:translate-y-0]",
                                "motion-duration-500ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "500ms");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-primary",
                                "motion-[0:translate-y-0,50:translate-y-[-20px],100:translate-y-0]",
                                "motion-duration-1000ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "1000ms");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-primary",
                                "motion-[0:translate-y-0,50:translate-y-[-20px],100:translate-y-0]",
                                "motion-duration-2000ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "2000ms");
                        });
                    });
                });

                // Easing functions
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H3, "mb-3"], "Easing Functions");
                    view.Row([Layout.Row.Lg, "flex-wrap items-end"], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-secondary",
                                "motion-[0:translate-x-0,50:translate-x-[30px],100:translate-x-0]",
                                "motion-duration-1500ms motion-loop motion-ease-linear"
                            ]);
                            view.Text([Text.Caption], "Linear");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-secondary",
                                "motion-[0:translate-x-0,50:translate-x-[30px],100:translate-x-0]",
                                "motion-duration-1500ms motion-loop motion-ease-ease-in"
                            ]);
                            view.Text([Text.Caption], "Ease In");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-secondary",
                                "motion-[0:translate-x-0,50:translate-x-[30px],100:translate-x-0]",
                                "motion-duration-1500ms motion-loop motion-ease-ease-out"
                            ]);
                            view.Text([Text.Caption], "Ease Out");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-secondary",
                                "motion-[0:translate-x-0,50:translate-x-[30px],100:translate-x-0]",
                                "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                            ]);
                            view.Text([Text.Caption], "Ease In-Out");
                        });
                    });
                });

                // Stagger timing
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H3, "mb-3"], "Stagger Timing");
                    view.Column([Layout.Column.Sm], content: view =>
                    {
                        view.Text([Text.Label], "30ms stagger");
                        view.Text([
                            Text.Body,
                            "motion-[0:opacity-0,50:opacity-100,100:opacity-0]",
                            "motion-duration-2000ms motion-stagger-30ms motion-per-letter-loop motion-fill-both"
                        ], "Fast stagger");

                        view.Text([Text.Label], "80ms stagger");
                        view.Text([
                            Text.Body,
                            "motion-[0:opacity-0,50:opacity-100,100:opacity-0]",
                            "motion-duration-2000ms motion-stagger-80ms motion-per-letter-loop motion-fill-both"
                        ], "Medium stagger");

                        view.Text([Text.Label], "150ms stagger");
                        view.Text([
                            Text.Body,
                            "motion-[0:opacity-0,50:opacity-100,100:opacity-0]",
                            "motion-duration-2000ms motion-stagger-150ms motion-per-letter-loop motion-fill-both"
                        ], "Slow stagger");
                    });
                });

                // Delay - converted to looping with different phase offsets
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.H3, "mb-3"], "Animation Delay");
                    view.Row([Layout.Row.Lg, "flex-wrap items-end"], content: view =>
                    {
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-accent",
                                "motion-[0:opacity-0_scale-[0.5],50:opacity-100_scale-100,100:opacity-0_scale-[0.5]]",
                                "motion-duration-2000ms motion-delay-0ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "0ms delay");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-accent",
                                "motion-[0:opacity-0_scale-[0.5],50:opacity-100_scale-100,100:opacity-0_scale-[0.5]]",
                                "motion-duration-2000ms motion-delay-200ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "200ms delay");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-accent",
                                "motion-[0:opacity-0_scale-[0.5],50:opacity-100_scale-100,100:opacity-0_scale-[0.5]]",
                                "motion-duration-2000ms motion-delay-500ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "500ms delay");
                        });
                        view.Column([Layout.Column.Xs, "items-center"], content: view =>
                        {
                            view.Box([
                                "w-12 h-12 rounded bg-accent",
                                "motion-[0:opacity-0_scale-[0.5],50:opacity-100_scale-100,100:opacity-0_scale-[0.5]]",
                                "motion-duration-2000ms motion-delay-1000ms motion-loop"
                            ]);
                            view.Text([Text.Caption], "1000ms delay");
                        });
                    });
                });
            });
        });
    }

    private void RenderChromaticAberrationEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Chromatic Aberration Effects");
            view.Text([Text.Caption, "mb-6"], "RGB channel separation effects using text-shadow");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Static chromatic aberration
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Static RGB Split");
                    view.Text([
                        "text-3xl font-bold text-white",
                        "text-shadow-[2px_0_0_rgba(255,0,0,0.7),-2px_0_0_rgba(0,255,255,0.7)]"
                    ], "CHROMATIC");
                });

                // Animated chromatic glitch
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Animated Glitch");
                    view.Text([
                        "text-3xl font-bold text-white",
                        "glitch:motion-[0:text-shadow-[0_0_0_transparent,0_0_0_transparent]," +
                        "20:text-shadow-[3px_0_0_rgba(255,0,0,0.8),-3px_0_0_rgba(0,255,255,0.8)]," +
                        "40:text-shadow-[-2px_1px_0_rgba(255,0,0,0.6),2px_-1px_0_rgba(0,255,255,0.6)]," +
                        "60:text-shadow-[2px_0_0_rgba(255,0,0,0.8),-2px_0_0_rgba(0,255,255,0.8)]," +
                        "80:text-shadow-[-1px_-1px_0_rgba(255,0,0,0.5),1px_1px_0_rgba(0,255,255,0.5)]," +
                        "100:text-shadow-[0_0_0_transparent,0_0_0_transparent]]",
                        "glitch:motion-duration-150ms glitch:motion-loop glitch:motion-ease-[steps(1)]"
                    ], "GLITCH");
                });

                // Smooth chromatic wave
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Smooth RGB Wave");
                    view.Text([
                        "text-3xl font-bold text-white",
                        "wave:motion-[0:text-shadow-[2px_0_0_rgba(255,0,0,0.6),-2px_0_0_rgba(0,255,255,0.6)]," +
                        "50:text-shadow-[-2px_0_0_rgba(255,0,0,0.6),2px_0_0_rgba(0,255,255,0.6)]," +
                        "100:text-shadow-[2px_0_0_rgba(255,0,0,0.6),-2px_0_0_rgba(0,255,255,0.6)]]",
                        "wave:motion-duration-2000ms wave:motion-loop wave:motion-ease-ease-in-out"
                    ], "WAVE");
                });

                // Glow with chromatic edges (inspired by Chat.cs)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Glow with Chromatic Burst");
                    view.Text([
                        "text-3xl font-bold text-rose-600",
                        "glow:motion-[0:text-shadow-[0_0_0_rgba(0,0,0,0)]," +
                        "25:text-shadow-[0_0_0.5em_rgba(255,0,128,0.5)]," +
                        "50:text-shadow-[0_0_0.8em_rgba(255,100,150,0.4)]," +
                        "75:text-shadow-[0.1em_0_0_rgba(255,0,100,0.6),-0.1em_0_0_rgba(255,150,200,0.6)]," +
                        "100:text-shadow-[0_0_0_rgba(0,0,0,0)]]",
                        "glow:motion-duration-3000ms glow:motion-loop"
                    ], "NEON GLOW");
                });

                // Per-letter chromatic stagger
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Per-Letter Chromatic Wave");
                    view.Text([
                        "text-3xl font-bold text-white",
                        "motion-[0:text-shadow-[0_0_0_transparent,0_0_0_transparent]," +
                        "50:text-shadow-[3px_0_0_rgba(255,0,0,0.8),-3px_0_0_rgba(0,255,255,0.8)]," +
                        "100:text-shadow-[0_0_0_transparent,0_0_0_transparent]]",
                        "motion-duration-800ms motion-stagger-50ms motion-per-letter-loop"
                    ], "RGB SPLIT");
                });

                // Subtle chromatic on hover simulation (using motion)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Subtle Chromatic Drift");
                    view.Text([
                        "text-2xl font-medium text-gray-200",
                        "drift:motion-[0:text-shadow-[1px_0_0_rgba(255,100,100,0.3),-1px_0_0_rgba(100,200,255,0.3)]," +
                        "33:text-shadow-[0_1px_0_rgba(255,100,100,0.3),0_-1px_0_rgba(100,200,255,0.3)]," +
                        "66:text-shadow-[-1px_0_0_rgba(255,100,100,0.3),1px_0_0_rgba(100,200,255,0.3)]," +
                        "100:text-shadow-[1px_0_0_rgba(255,100,100,0.3),-1px_0_0_rgba(100,200,255,0.3)]]",
                        "drift:motion-duration-4000ms drift:motion-loop drift:motion-ease-ease-in-out"
                    ], "Subtle Effect");
                });
            });
        });
    }

    private void RenderFilterEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Filter Effects");
            view.Text([Text.Caption, "mb-6"], "CSS filter animations: blur, brightness, hue-rotate, saturate, and more");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Hue rotation cycle - rainbow color shifting
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Hue Rotate Cycle");
                    view.Row(["gap-4 items-center"], content: view =>
                    {
                        view.Box([
                            "w-16 h-16 rounded-lg bg-gradient-to-br from-red-500 to-orange-500",
                            "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]]",
                            "motion-duration-3000ms motion-loop motion-ease-linear"
                        ]);
                        view.Text([
                            "text-2xl font-bold bg-gradient-to-r from-purple-500 to-pink-500 bg-clip-text text-transparent",
                            "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]]",
                            "motion-duration-4000ms motion-loop motion-ease-linear"
                        ], "Rainbow Text");
                    });
                });

                // Blur breathe - soft focus pulse
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Blur Breathe");
                    view.Row(["gap-4 items-center"], content: view =>
                    {
                        view.Box([
                            "w-16 h-16 rounded-full bg-gradient-to-br from-blue-400 to-cyan-300",
                            "motion-[0:blur-0,50:blur-[4px],100:blur-0]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ]);
                        view.Text([
                            "text-2xl font-semibold text-blue-600",
                            "motion-[0:blur-0,50:blur-[2px],100:blur-0]",
                            "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                        ], "Dreamy Focus");
                    });
                });

                // Brightness flash - lightning/flash effect
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Brightness Flash");
                    view.Row(["gap-4 items-center"], content: view =>
                    {
                        view.Box([
                            "w-16 h-16 rounded bg-amber-500",
                            "motion-[0:brightness-100,15:brightness-[2],30:brightness-100,100:brightness-100]",
                            "motion-duration-2000ms motion-loop"
                        ]);
                        view.Text([
                            "text-2xl font-bold text-yellow-500",
                            "motion-[0:brightness-100,10:brightness-[2.5],20:brightness-100,100:brightness-100]",
                            "motion-duration-3000ms motion-loop"
                        ], "⚡ FLASH ⚡");
                    });
                });

                // Saturate pulse - vibrance boost
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Saturate Pulse");
                    view.Row(["gap-4 items-center"], content: view =>
                    {
                        view.Box([
                            "w-16 h-16 rounded-lg bg-gradient-to-br from-green-400 to-emerald-600",
                            "motion-[0:saturate-100,50:saturate-[2],100:saturate-100]",
                            "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                        ]);
                        view.Text([
                            "text-2xl font-bold text-emerald-500",
                            "motion-[0:saturate-50,50:saturate-[1.8],100:saturate-50]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ], "VIBRANT");
                    });
                });

                // Grayscale transition - color to B&W
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Grayscale Fade");
                    view.Row(["gap-4 items-center"], content: view =>
                    {
                        view.Box([
                            "w-16 h-16 rounded bg-gradient-to-br from-pink-500 via-purple-500 to-indigo-500",
                            "motion-[0:grayscale-0,50:grayscale-100,100:grayscale-0]",
                            "motion-duration-4000ms motion-loop motion-ease-ease-in-out"
                        ]);
                        view.Text([
                            "text-2xl font-semibold bg-gradient-to-r from-rose-500 to-violet-500 bg-clip-text text-transparent",
                            "motion-[0:grayscale-0,50:grayscale-100,100:grayscale-0]",
                            "motion-duration-5000ms motion-loop motion-ease-ease-in-out"
                        ], "Color Drain");
                    });
                });

                // Combined filter effects
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Combined: Blur + Brightness + Hue");
                    view.Box([
                        "w-24 h-24 rounded-xl bg-gradient-to-br from-violet-500 to-fuchsia-500",
                        "motion-[0:blur-0_brightness-100_hue-rotate-0," +
                        "25:blur-[2px]_brightness-[1.2]_hue-rotate-[45deg]," +
                        "50:blur-[4px]_brightness-[1.5]_hue-rotate-[90deg]," +
                        "75:blur-[2px]_brightness-[1.2]_hue-rotate-[135deg]," +
                        "100:blur-0_brightness-100_hue-rotate-[180deg]]",
                        "motion-duration-4000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });
            });
        });
    }

    private void RenderShadowEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Shadow Effects");
            view.Text([Text.Caption, "mb-6"], "Shadow animations using blur, scale, opacity, and hue-rotate (CSS variables for shadows don't interpolate)");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Floating lift - use translate + scale to simulate shadow growth
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Floating Lift (translate + blur shadow layer)");
                    view.Box(["relative w-24 h-24"], content: v =>
                    {
                        // Shadow layer (blurred copy)
                        v.Box([
                            "absolute inset-0 rounded-xl bg-black/20 blur-md",
                            "motion-[0:translate-y-[4px]_scale-100,50:translate-y-[16px]_scale-[1.1],100:translate-y-[4px]_scale-100]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ]);
                        // Main element
                        v.Box([
                            "absolute inset-0 rounded-xl bg-white",
                            "motion-[0:translate-y-0,50:translate-y-[-8px],100:translate-y-0]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ]);
                    });
                });

                // Pulsing glow - use blur + opacity on a colored layer
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Pulsing Glow (blur + opacity layer)");
                    view.Box(["relative w-24 h-24"], content: v =>
                    {
                        // Glow layer
                        v.Box([
                            "absolute inset-[-10px] rounded-full bg-blue-500",
                            "motion-[0:blur-[5px]_opacity-0,50:blur-[20px]_opacity-60,100:blur-[5px]_opacity-0]",
                            "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                        ]);
                        // Main element
                        v.Box(["absolute inset-0 rounded-full bg-gradient-to-br from-cyan-400 to-blue-500"]);
                    });
                });

                // Neon glow text - use blur + opacity on duplicate text
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Neon Glow Text (blur layer)");
                    view.Box(["relative"], content: v =>
                    {
                        // Glow layer (blurred text behind)
                        v.Text([
                            "text-3xl font-bold text-pink-400 absolute blur-sm",
                            "motion-[0:blur-[4px]_opacity-50,50:blur-[12px]_opacity-80,100:blur-[4px]_opacity-50]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ], "NEON");
                        // Main text
                        v.Text(["text-3xl font-bold text-pink-400 relative"], "NEON");
                    });
                });

                // Color cycling glow - use hue-rotate on a static colored shadow
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Color Glow Cycle (hue-rotate)");
                    view.Box(["relative w-24 h-24"], content: v =>
                    {
                        // Glow layer with hue-rotate
                        v.Box([
                            "absolute inset-[-10px] rounded-full bg-red-500 blur-xl opacity-60",
                            "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]]",
                            "motion-duration-3000ms motion-loop motion-ease-linear"
                        ]);
                        // Main element
                        v.Box(["absolute inset-0 rounded-full bg-white"]);
                    });
                });

                // Shadow orbit - use positioned shadow layer with translate
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Shadow Orbit (moving shadow layer)");
                    view.Box(["relative w-20 h-20"], content: v =>
                    {
                        // Shadow layer
                        v.Box([
                            "absolute inset-0 rounded-lg bg-black/30 blur-md",
                            "motion-[0:translate-x-[5px]_translate-y-[5px]," +
                            "25:translate-x-[-5px]_translate-y-[5px]," +
                            "50:translate-x-[-5px]_translate-y-[-5px]," +
                            "75:translate-x-[5px]_translate-y-[-5px]," +
                            "100:translate-x-[5px]_translate-y-[5px]]",
                            "motion-duration-3000ms motion-loop motion-ease-linear"
                        ]);
                        // Main element
                        v.Box(["absolute inset-0 rounded-lg bg-amber-100"]);
                    });
                });

                // Breathing shadow - scale + opacity
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Breathing Shadow (scale + opacity)");
                    view.Box(["relative w-24 h-24"], content: v =>
                    {
                        // Shadow layer
                        v.Box([
                            "absolute inset-0 rounded-xl bg-purple-600 blur-lg",
                            "motion-[0:scale-100_opacity-30,50:scale-[1.15]_opacity-50,100:scale-100_opacity-30]",
                            "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                        ]);
                        // Main element
                        v.Box(["absolute inset-0 rounded-xl bg-gradient-to-br from-violet-500 to-purple-600"]);
                    });
                });
            });
        });
    }

    private void RenderCreativeEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Creative Effects");
            view.Text([Text.Caption, "mb-6"], "Combined transforms, filters, and creative animations");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Heartbeat - scale with shadow
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Heartbeat");
                    view.Text([
                        "text-4xl",
                        "motion-[0:scale-100,14:scale-[1.3],28:scale-100,42:scale-[1.15],56:scale-100,100:scale-100]",
                        "motion-duration-1000ms motion-loop"
                    ], "❤️");
                });

                // Morphing blob
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Morphing Shape");
                    view.Box([
                        "w-20 h-20 bg-gradient-to-br from-purple-500 to-pink-500",
                        "motion-[0:rounded-[30%_70%_70%_30%/30%_30%_70%_70%]," +
                        "25:rounded-[70%_30%_30%_70%/70%_70%_30%_30%]," +
                        "50:rounded-[30%_70%_70%_30%/70%_30%_70%_30%]," +
                        "75:rounded-[70%_30%_30%_70%/30%_70%_30%_70%]," +
                        "100:rounded-[30%_70%_70%_30%/30%_30%_70%_70%]]",
                        "motion-duration-4000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // Typing cursor blink
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Cursor Blink");
                    view.Row(["items-center"], content: view =>
                    {
                        view.Text(["text-2xl font-mono text-green-400"], "command_");
                        view.Box([
                            "w-3 h-7 bg-green-400",
                            "motion-[0:opacity-100,50:opacity-0,100:opacity-100]",
                            "motion-duration-1000ms motion-loop motion-ease-[steps(1)]"
                        ]);
                    });
                });

                // Shake/Wiggle
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Wiggle");
                    view.Text([
                        "text-3xl",
                        "motion-[0:rotate-0,25:rotate-[-5deg],50:rotate-0,75:rotate-[5deg],100:rotate-0]",
                        "motion-duration-300ms motion-loop"
                    ], "👋");
                });

                // Bounce with squash/stretch
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Squash & Stretch");
                    view.Box([
                        "w-12 h-12 rounded-full bg-orange-500",
                        "motion-[0:translate-y-0_scale-x-100_scale-y-100," +
                        "30:translate-y-[-30px]_scale-x-[0.9]_scale-y-[1.1]," +
                        "50:translate-y-[-40px]_scale-x-100_scale-y-100," +
                        "70:translate-y-[-30px]_scale-x-[0.9]_scale-y-[1.1]," +
                        "85:translate-y-0_scale-x-[1.2]_scale-y-[0.8]," +
                        "100:translate-y-0_scale-x-100_scale-y-100]",
                        "motion-duration-800ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // Glitch with skew
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Skew Glitch");
                    view.Text([
                        "text-2xl font-bold text-red-500",
                        "motion-[0:skew-x-0_translate-x-0," +
                        "20:skew-x-[10deg]_translate-x-[2px]," +
                        "40:skew-x-[-5deg]_translate-x-[-2px]," +
                        "60:skew-x-[3deg]_translate-x-[1px]," +
                        "80:skew-x-[-2deg]_translate-x-[-1px]," +
                        "100:skew-x-0_translate-x-0]",
                        "motion-duration-200ms motion-loop motion-ease-[steps(1)]"
                    ], "ERROR");
                });

                // Pendulum swing
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Pendulum");
                    view.Box(["flex justify-center"], content: view =>
                    {
                        view.Box([
                            "w-1 h-16 bg-gray-400 origin-top",
                            "motion-[0:rotate-[-30deg],50:rotate-[30deg],100:rotate-[-30deg]]",
                            "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                        ], content: view =>
                        {
                            view.Box(["w-6 h-6 rounded-full bg-gray-600 absolute -bottom-3 -left-2.5"]);
                        });
                    });
                });
            });
        });
    }

    private void Render3DTransformEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "3D Transform Effects");
            view.Text([Text.Caption, "mb-6"], "Perspective and 3D rotation animations");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Card flip (Y-axis)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Card Flip (Y-axis)");
                    view.Box([
                        "w-24 h-32 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center",
                        "motion-[0:rotate-y-0,50:rotate-y-[180deg],100:rotate-y-[360deg]]",
                        "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                    ], content: v => v.Text([Text.H3, "text-white"], "FLIP"));
                });

                // Card flip (X-axis)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Card Flip (X-axis)");
                    view.Box([
                        "w-24 h-32 rounded-xl bg-gradient-to-br from-teal-500 to-cyan-600 flex items-center justify-center",
                        "motion-[0:rotate-x-0,50:rotate-x-[180deg],100:rotate-x-[360deg]]",
                        "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                    ], content: v => v.Text([Text.H3, "text-white"], "TILT"));
                });

                // Coin spin (combined X + Z rotation for realistic coin flip)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Coin Spin");
                    view.Box([
                        "w-16 h-16 rounded-full bg-gradient-to-br from-yellow-400 to-amber-500 flex items-center justify-center shadow-lg",
                        "motion-[0:rotate-y-0,100:rotate-y-[1080deg]]",
                        "motion-duration-2000ms motion-loop motion-ease-linear"
                    ], content: v => v.Text(["text-2xl font-bold text-yellow-800"], "$"));
                });

                // Swing door effect
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Swing Door");
                    view.Box([
                        "w-24 h-32 rounded-lg bg-gradient-to-br from-amber-600 to-orange-700 origin-left",
                        "motion-[0:rotate-y-0,100:rotate-y-[-70deg]]",
                        "motion-duration-2000ms motion-ping-pong motion-ease-ease-in-out"
                    ], content: v =>
                    {
                        v.Box(["w-3 h-3 rounded-full bg-amber-300 absolute right-2 top-1/2 -translate-y-1/2"]);
                    });
                });

                // Falling card effect
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Falling Card");
                    view.Box([
                        "w-20 h-28 rounded-lg bg-gradient-to-br from-rose-500 to-red-600 origin-top",
                        "motion-[0:rotate-x-0_translate-y-0,50:rotate-x-[30deg]_translate-y-[10px],100:rotate-x-0_translate-y-0]",
                        "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // 3D cube rotation simulation (single face)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Cube Face Rotate");
                    view.Box([
                        "w-20 h-20 rounded bg-gradient-to-br from-emerald-400 to-green-600 flex items-center justify-center",
                        "motion-[0:rotate-x-0_rotate-y-0," +
                        "25:rotate-x-[90deg]_rotate-y-0," +
                        "50:rotate-x-[90deg]_rotate-y-[90deg]," +
                        "75:rotate-x-0_rotate-y-[90deg]," +
                        "100:rotate-x-0_rotate-y-0]",
                        "motion-duration-4000ms motion-loop motion-ease-ease-in-out"
                    ], content: v => v.Text([Text.H3, "text-white"], "3D"));
                });

                // Wobble plate (X + Y slight oscillation)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Wobble Plate");
                    view.Box([
                        "w-24 h-24 rounded-full bg-gradient-to-br from-slate-300 to-gray-400 shadow-inner",
                        "motion-[0:rotate-x-[5deg]_rotate-y-0," +
                        "25:rotate-x-0_rotate-y-[5deg]," +
                        "50:rotate-x-[-5deg]_rotate-y-0," +
                        "75:rotate-x-0_rotate-y-[-5deg]," +
                        "100:rotate-x-[5deg]_rotate-y-0]",
                        "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });
            });
        });
    }

    private void RenderRetroEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Retro & CRT Effects");
            view.Text([Text.Caption, "mb-6"], "VHS, CRT scanlines, and vintage display aesthetics");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // CRT Scanlines with moving flicker band
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "CRT Scanlines + Flicker");
                    view.Box([
                        "w-48 h-32 rounded bg-black flex items-center justify-center overflow-hidden relative"
                    ], content: v =>
                    {
                        // Scanline overlay (static)
                        v.Box(["absolute inset-0 bg-[repeating-linear-gradient(0deg,transparent_0px,transparent_2px,rgba(0,255,0,0.03)_2px,rgba(0,255,0,0.03)_4px)] pointer-events-none"]);
                        // Moving flicker band
                        v.Box([
                            "absolute inset-x-0 h-4 bg-gradient-to-b from-transparent via-green-500/10 to-transparent",
                            "motion-[0:translate-y-[-50px],100:translate-y-[150px]]",
                            "motion-duration-3000ms motion-loop motion-ease-linear"
                        ]);
                        v.Text(["text-green-400 font-mono text-lg relative z-10"], "SIGNAL OK");
                    });
                });

                // VHS Tracking Jump - mostly stable, occasional jitter
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "VHS Tracking Jump");
                    view.Text([
                        "text-2xl font-bold text-white bg-black px-4 py-2 rounded",
                        "motion-[0:translate-x-0_skew-x-0,92:translate-x-0_skew-x-0,94:translate-x-[3px]_skew-x-[2deg],96:translate-x-[-2px]_skew-x-[-1deg],98:translate-x-[1px]_skew-x-[0.5deg],100:translate-x-0_skew-x-0]",
                        "motion-duration-3000ms motion-loop"
                    ], "PLAY ▶");
                });

                // Signal Lock (glitch to stable, looped for demo)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Signal Lock (Glitch → Stable → Loop)");
                    view.Text([
                        "text-2xl font-bold text-cyan-400",
                        "lock:motion-[0:blur-[4px]_translate-x-[-4px]_skew-x-[5deg]_opacity-70," +
                        "15:blur-[3px]_translate-x-[3px]_skew-x-[-3deg]_opacity-80," +
                        "30:blur-[2px]_translate-x-[-2px]_skew-x-[2deg]_opacity-85," +
                        "45:blur-[1px]_translate-x-[1px]_skew-x-[-1deg]_opacity-90," +
                        "60:blur-0_translate-x-0_skew-x-0_opacity-100," +
                        "90:blur-0_translate-x-0_skew-x-0_opacity-100," +
                        "100:blur-[4px]_translate-x-[-4px]_skew-x-[5deg]_opacity-70]",
                        "lock:motion-duration-3000ms lock:motion-loop",
                        "rgb:motion-[0:text-shadow-[3px_0_0_rgba(255,0,0,0.7),-3px_0_0_rgba(0,255,255,0.7)]," +
                        "40:text-shadow-[1px_0_0_rgba(255,0,0,0.3),-1px_0_0_rgba(0,255,255,0.3)]," +
                        "60:text-shadow-none," +
                        "90:text-shadow-none," +
                        "100:text-shadow-[3px_0_0_rgba(255,0,0,0.7),-3px_0_0_rgba(0,255,255,0.7)]]",
                        "rgb:motion-duration-3000ms rgb:motion-loop"
                    ], "LOCKED");
                });

                // Pixel/Digital Step Stutter
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Digital Stutter");
                    view.Text([
                        "text-xl font-mono text-lime-400 bg-gray-900 px-3 py-1 rounded",
                        "motion-[0:translate-x-0_opacity-100,25:translate-x-[2px]_opacity-80,50:translate-x-[-1px]_opacity-90,75:translate-x-[1px]_opacity-85,100:translate-x-0_opacity-100]",
                        "motion-duration-150ms motion-loop motion-ease-[steps(4)]"
                    ], "LOADING...");
                });

                // Interference band sweep
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Interference Sweep");
                    view.Box([
                        "w-48 h-24 rounded bg-gradient-to-b from-gray-800 to-gray-900 flex items-center justify-center relative overflow-hidden"
                    ], content: v =>
                    {
                        v.Box([
                            "absolute inset-x-0 h-8 bg-gradient-to-b from-transparent via-white/10 to-transparent",
                            "motion-[0:translate-y-[-100px],100:translate-y-[100px]]",
                            "motion-duration-2000ms motion-loop motion-ease-linear"
                        ]);
                        v.Text(["text-white font-mono relative z-10"], "BROADCAST");
                    });
                });

                // Old TV turn on/off effect (looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "TV Power On/Off Cycle");
                    view.Box([
                        "w-40 h-28 rounded bg-black flex items-center justify-center overflow-hidden",
                        "motion-[0:scale-x-0_scale-y-[0.02]_brightness-[3]," +
                        "15:scale-x-[1.1]_scale-y-[0.02]_brightness-[2]," +
                        "30:scale-x-100_scale-y-[0.5]_brightness-[1.5]," +
                        "50:scale-x-100_scale-y-100_brightness-100," +
                        "70:scale-x-100_scale-y-100_brightness-100," +
                        "85:scale-x-100_scale-y-[0.5]_brightness-[1.5]," +
                        "95:scale-x-[1.1]_scale-y-[0.02]_brightness-[2]," +
                        "100:scale-x-0_scale-y-[0.02]_brightness-[3]]",
                        "motion-duration-4000ms motion-loop"
                    ], content: v => v.Text(["text-blue-300 text-sm"], "CHANNEL 3"));
                });
            });
        });
    }

    private void RenderRevealEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Reveal & Intro Effects");
            view.Text([Text.Caption, "mb-6"], "Cinematic reveal animations for hero text and content");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Ink bloom / soft focus reveal (looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Ink Bloom Reveal (Loop)");
                    view.Text([
                        "text-3xl font-bold text-white",
                        "motion-[0:opacity-0_blur-[12px]_scale-[1.02]," +
                        "25:opacity-70_blur-[4px]_scale-[1.01]_contrast-[1.2]," +
                        "40:opacity-100_blur-0_scale-100_contrast-100," +
                        "70:opacity-100_blur-0_scale-100_contrast-100," +
                        "85:opacity-70_blur-[4px]_scale-[1.01]," +
                        "100:opacity-0_blur-[12px]_scale-[1.02]]",
                        "motion-duration-3000ms motion-loop"
                    ], "CINEMATIC");
                });

                // Scan reveal (wipe with flicker, looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Scan Wipe Reveal (Loop)");
                    view.Box([
                        "w-56 h-16 rounded-lg bg-gradient-to-r from-purple-600 to-pink-500 flex items-center justify-center relative overflow-hidden"
                    ], content: v =>
                    {
                        v.Box([
                            "absolute inset-0 bg-black origin-left",
                            "motion-[0:scale-x-100,5:scale-x-100_opacity-80,15:scale-x-[0.7]_opacity-100,25:scale-x-[0.4]_opacity-85,40:scale-x-0_opacity-100,70:scale-x-0_opacity-100,85:scale-x-[0.5]_opacity-90,100:scale-x-100]",
                            "motion-duration-3000ms motion-loop"
                        ]);
                        v.Text(["text-white font-bold relative z-10"], "REVEALED");
                    });
                });

                // Split-flap / flipboard letter reveal (looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Flipboard Cascade (Loop)");
                    view.Text([
                        "text-3xl font-mono font-bold text-amber-400",
                        "motion-[0:rotate-x-[-90deg]_opacity-0,30:rotate-x-[10deg]_opacity-100,40:rotate-x-[-5deg],50:rotate-x-0_opacity-100,80:rotate-x-0_opacity-100,90:rotate-x-[10deg]_opacity-50,100:rotate-x-[-90deg]_opacity-0]",
                        "motion-duration-2000ms motion-stagger-80ms motion-per-letter-loop motion-ease-ease-out"
                    ], "ARRIVALS");
                });

                // Depth Z-pop wave (letters come forward)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Depth Pop Wave");
                    view.Text([
                        "text-2xl font-bold text-blue-400",
                        "motion-[0:translate-z-[-50px]_blur-[3px]_opacity-50_scale-[0.95]," +
                        "50:translate-z-[10px]_blur-0_opacity-100_scale-[1.02]," +
                        "100:translate-z-0_blur-0_opacity-100_scale-100]",
                        "motion-duration-600ms motion-stagger-40ms motion-per-letter-loop motion-ease-ease-out"
                    ], "DEPTH");
                });

                // Highlight scan (per-letter glow pass) - using brightness + scale
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Highlight Scan");
                    view.Text([
                        "text-2xl font-semibold text-gray-400",
                        "motion-[0:brightness-100_scale-100," +
                        "40:brightness-[2]_scale-[1.1]," +
                        "100:brightness-100_scale-100]",
                        "motion-duration-800ms motion-stagger-50ms motion-per-letter-loop"
                    ], "SCANNING");
                });

                // Elastic pop in (looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Elastic Pop In (Loop)");
                    view.Text([
                        "text-3xl font-black text-rose-500",
                        "motion-[0:scale-0_rotate-[-45deg]_opacity-0," +
                        "25:scale-[1.2]_rotate-[10deg]_opacity-100," +
                        "35:scale-[0.9]_rotate-[-5deg]," +
                        "45:scale-100_rotate-0," +
                        "70:scale-100_rotate-0_opacity-100," +
                        "85:scale-[0.9]_rotate-[10deg]_opacity-50," +
                        "100:scale-0_rotate-[-45deg]_opacity-0]",
                        "motion-duration-2500ms motion-stagger-60ms motion-per-letter-loop motion-ease-ease-out"
                    ], "BOUNCE");
                });

                // Glitch stabilize (looped)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Glitch Stabilize (Loop)");
                    view.Text([
                        "text-2xl font-bold text-emerald-400",
                        "motion-[0:translate-x-[-5px]_opacity-0," +
                        "8:translate-x-[4px]_opacity-40," +
                        "16:translate-x-[-3px]_opacity-60," +
                        "24:translate-x-[2px]_opacity-75," +
                        "35:translate-x-0_opacity-100," +
                        "70:translate-x-0_opacity-100," +
                        "80:translate-x-[2px]_opacity-75," +
                        "90:translate-x-[-3px]_opacity-40," +
                        "100:translate-x-[-5px]_opacity-0]",
                        "motion-duration-2000ms motion-stagger-30ms motion-per-letter-loop"
                    ], "STABLE");
                });
            });
        });
    }

    private void RenderImageFilterEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Image Filter Effects");
            view.Text([Text.Caption, "mb-6"], "Filter animations for images and containers");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Duotone / palette cycle
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Duotone Palette Cycle");
                    view.Box([
                        "w-32 h-32 rounded-xl bg-gradient-to-br from-sky-400 via-violet-500 to-fuchsia-500",
                        "motion-[0:hue-rotate-0_saturate-100_contrast-100," +
                        "25:hue-rotate-[45deg]_saturate-[1.3]_contrast-[1.1]," +
                        "50:hue-rotate-[90deg]_saturate-[1.5]_contrast-100," +
                        "75:hue-rotate-[135deg]_saturate-[1.2]_contrast-[1.05]," +
                        "100:hue-rotate-[180deg]_saturate-100_contrast-100]",
                        "motion-duration-6000ms motion-loop motion-ease-linear"
                    ]);
                });

                // Sepia fade (vintage photo)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Vintage Photo Fade");
                    view.Box([
                        "w-32 h-32 rounded-lg bg-gradient-to-br from-amber-200 via-orange-300 to-red-400",
                        "motion-[0:sepia-0_brightness-100,50:sepia-100_brightness-[0.9]_contrast-[1.1],100:sepia-0_brightness-100]",
                        "motion-duration-4000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // Invert flash (negative image)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Negative Flash");
                    view.Box([
                        "w-32 h-32 rounded-lg bg-gradient-to-br from-blue-500 to-green-400",
                        "motion-[0:invert-0,45:invert-0,50:invert-100,55:invert-0,100:invert-0]",
                        "motion-duration-3000ms motion-loop"
                    ]);
                });

                // Depth of field (blur pulse)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Depth of Field Shift");
                    view.Row(["gap-4"], content: view =>
                    {
                        view.Box([
                            "w-20 h-20 rounded-full bg-red-500",
                            "motion-[0:blur-[8px]_opacity-60,50:blur-0_opacity-100,100:blur-[8px]_opacity-60]",
                            "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                        ]);
                        view.Box([
                            "w-20 h-20 rounded-full bg-green-500",
                            "motion-[0:blur-0_opacity-100,50:blur-[8px]_opacity-60,100:blur-0_opacity-100]",
                            "motion-duration-3000ms motion-loop motion-ease-ease-in-out"
                        ]);
                    });
                });

                // Drop shadow color shift (using hue-rotate on colored shadow)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Shadow Color Shift (Hue Rotate)");
                    view.Box([
                        "w-24 h-24 rounded-xl bg-white shadow-[0_8px_24px_rgba(255,0,0,0.6)]",
                        "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]]",
                        "motion-duration-4000ms motion-loop motion-ease-linear"
                    ]);
                });

                // Contrast punch (dramatic)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Contrast Punch");
                    view.Box([
                        "w-32 h-24 rounded-lg bg-gradient-to-r from-gray-600 via-gray-400 to-gray-600",
                        "motion-[0:contrast-100_brightness-100,20:contrast-[1.5]_brightness-[1.3],40:contrast-100_brightness-100,100:contrast-100_brightness-100]",
                        "motion-duration-2000ms motion-loop"
                    ]);
                });

                // Film grain simulation (visible noise texture)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Film Grain Noise");
                    view.Box([
                        "w-40 h-28 rounded-lg bg-gradient-to-br from-amber-100 to-orange-200 relative overflow-hidden"
                    ], content: v =>
                    {
                        // Multiple noise layers for visible grain effect
                        v.Box([
                            "absolute inset-0 bg-[radial-gradient(circle_at_50%_50%,rgba(0,0,0,0.15)_1px,transparent_1px)] bg-[length:3px_3px]",
                            "motion-[0:translate-x-0_translate-y-0,25:translate-x-[1px]_translate-y-[1px],50:translate-x-0_translate-y-[2px],75:translate-x-[2px]_translate-y-0,100:translate-x-0_translate-y-0]",
                            "motion-duration-150ms motion-loop motion-ease-[steps(4)]"
                        ]);
                        v.Box([
                            "absolute inset-0 bg-[radial-gradient(circle_at_30%_70%,rgba(0,0,0,0.1)_1px,transparent_1px)] bg-[length:5px_5px]",
                            "motion-[0:opacity-60,50:opacity-30,100:opacity-60]",
                            "motion-duration-80ms motion-loop motion-ease-[steps(3)]"
                        ]);
                        v.Text(["text-stone-700 font-bold text-lg relative z-10 p-2"], "VINTAGE");
                    });
                });
            });
        });
    }

    private void RenderBorderEffects(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H2, "mb-4"], "Border & Outline Effects");
            view.Text([Text.Caption, "mb-6"], "Animated borders, outlines, and ring effects");

            view.Column([Layout.Column.Lg], content: view =>
            {
                // Border width pulse
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Border Width Pulse");
                    view.Box([
                        "w-20 h-20 rounded-lg bg-transparent border-blue-500",
                        "motion-[0:border-2,50:border-8,100:border-2]",
                        "motion-duration-1500ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // Border color cycle
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Border Color Shift");
                    view.Box([
                        "w-20 h-20 rounded-lg bg-transparent border-4",
                        "motion-[0:border-red-500,25:border-yellow-500,50:border-green-500,75:border-blue-500,100:border-red-500]",
                        "motion-duration-3000ms motion-loop motion-ease-linear"
                    ]);
                });

                // Ring pulse effect
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Ring Pulse");
                    view.Box([
                        "w-16 h-16 rounded-full bg-violet-500",
                        "motion-[0:ring-0_ring-violet-400,50:ring-8_ring-violet-400/50,100:ring-0_ring-violet-400]",
                        "motion-duration-1500ms motion-loop motion-ease-ease-out"
                    ]);
                });

                // Outline offset animation
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Outline Expand");
                    view.Box([
                        "w-20 h-20 rounded-lg bg-emerald-500 outline outline-2 outline-emerald-300",
                        "motion-[0:outline-offset-0,50:outline-offset-8,100:outline-offset-0]",
                        "motion-duration-1200ms motion-loop motion-ease-ease-in-out"
                    ]);
                });

                // Dashed border rotation effect (using gradient)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Rotating Dashes (via Spin)");
                    view.Box([
                        "w-20 h-20 rounded-full border-4 border-dashed border-orange-500",
                        "motion-[0:rotate-0,100:rotate-[360deg]]",
                        "motion-duration-4000ms motion-loop motion-ease-linear"
                    ]);
                });

                // Multi-ring ripple (stacked elements approach simulation)
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Focus Ripple");
                    view.Box([
                        "w-16 h-16 rounded-xl bg-sky-500",
                        "motion-[0:shadow-[0_0_0_0_rgba(56,189,248,0.7)],100:shadow-[0_0_0_15px_rgba(56,189,248,0)]]",
                        "motion-duration-1500ms motion-loop motion-ease-ease-out"
                    ]);
                });

                // Border radius morph
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Border Radius Morph");
                    view.Box([
                        "w-20 h-20 bg-gradient-to-br from-fuchsia-500 to-pink-500",
                        "motion-[0:rounded-none,25:rounded-tl-3xl,50:rounded-3xl,75:rounded-br-3xl,100:rounded-none]",
                        "motion-duration-2000ms motion-loop motion-ease-ease-in-out"
                    ]);
                });
            });
        });
    }
}
