return await App.Run(args);

// Determines which clients share an app instance. `UserId` is auto-filled per authenticated
// user (so each user gets their own instance); add more fields to partition further, or
// leave the record empty to have every client share a single instance.
public record SessionIdentity(string UserId);

// Per-client data, auto-mapped from URL query params (e.g. `?name=Alice` -> `Name`).
public record ClientParameters();

[App]
public partial class DepthParallaxApp(IApp<SessionIdentity, ClientParameters> app)
{
    private const string DefaultPrompt =
        "A misty forest gorge at golden hour: mossy boulders and fern fronds in the immediate "
        + "foreground, a wooden footbridge over a river in the midground, and pine-covered cliffs "
        + "fading into distant haze, strong layered depth, volumetric sunlight, cinematic wide shot";

    private UI UI { get; } = new(app, new IkonTheme());

    private readonly Reactive<string> _prompt = new(DefaultPrompt);
    private readonly Reactive<string?> _imageDataUri = new(null);
    private readonly Reactive<string?> _depthDataUri = new(null);
    private readonly Reactive<Stage> _stage = new(Stage.Idle);
    private readonly Reactive<string?> _error = new(null);
    private readonly Reactive<bool> _showDepth = new(false);
    private readonly Reactive<float> _parallaxStrength = new(0.025f);
    private readonly Reactive<int> _steps = new(16);
    private readonly Reactive<ParallaxAlgorithm> _algorithm = new(ParallaxAlgorithm.OcclusionMapping);
    private readonly Reactive<ImageGeneratorModel> _imageModel = new(ImageGeneratorModel.Gemini31FlashImage);
    private readonly Reactive<DepthEstimatorModel> _depthModel = new(DepthEstimatorModel.DepthAnythingV2);

    private bool IsGenerating => _stage.Value is Stage.GeneratingImage or Stage.EstimatingDepth;

    public async Task Main()
    {
        app.ClientJoinedAsync += async args =>
        {
            if (args.ClientContext.Theme != Constants.DarkTheme)
            {
                await ClientFunctions.SetThemeAsync(Constants.DarkTheme);
            }
        };

        UI.Root([Page.Default], content: view =>
        {
            view.Box([
                "relative min-h-screen w-full overflow-hidden",
                "bg-[radial-gradient(ellipse_at_center,#0a1220_0%,#050810_60%,#02040a_100%)]"
            ], content: view =>
            {
                RenderAurora(view);

                view.Column([
                    "relative z-10 w-full max-w-5xl mx-auto px-6 py-12 flex flex-col items-stretch gap-6"
                ], content: view =>
                {
                    view.Text([
                        "text-center text-5xl md:text-7xl font-black tracking-tight",
                        "text-transparent bg-clip-text bg-[linear-gradient(90deg,#a5f3fc_0%,#c084fc_50%,#22d3ee_100%)]",
                        "motion-[0:hue-rotate-0,100:hue-rotate-[360deg]] motion-duration-6000ms motion-loop motion-ease-linear"
                    ], "Depth Parallax");

                    view.Text(["text-center text-white/50 text-sm md:text-base -mt-2"],
                        "Prompt to image, then a depth map, then a live mouse-driven parallax shader.");

                    RenderModelPickers(view);
                    RenderPromptRow(view);
                    RenderStatus(view);
                    RenderResult(view);
                });
            });
        });
    }

    private void RenderAurora(IView view)
    {
        view.Box([
            "pointer-events-none absolute inset-0 z-0",
            "bg-[radial-gradient(ellipse_at_25%_15%,rgba(34,211,238,0.16)_0%,transparent_55%)]",
            "motion-[0:opacity-50,50:opacity-100,100:opacity-50] motion-duration-7000ms motion-loop motion-ease-ease-in-out"
        ]);

        view.Box([
            "pointer-events-none absolute inset-0 z-0",
            "bg-[radial-gradient(ellipse_at_80%_80%,rgba(192,132,252,0.14)_0%,transparent_55%)]",
            "motion-[0:opacity-30,50:opacity-80,100:opacity-30] motion-duration-9000ms motion-delay-1200ms motion-loop motion-ease-ease-in-out"
        ]);

        view.Box([
            "pointer-events-none absolute inset-0 z-0",
            "bg-[radial-gradient(ellipse_at_55%_50%,rgba(56,189,248,0.08)_0%,transparent_60%)]",
            "motion-[0:opacity-40,50:opacity-90,100:opacity-40] motion-duration-11000ms motion-delay-600ms motion-loop motion-ease-ease-in-out"
        ]);
    }

    private void RenderModelPickers(IView view)
    {
        view.Row(["gap-4 w-full"], content: view =>
        {
            view.Box(["flex-1"], content: v => v.Select(
                label: "Image model",
                value: _imageModel.Value.ToString(),
                disabled: IsGenerating,
                triggerStyle: ["w-full"],
                options: [.. Enum.GetValues<ImageGeneratorModel>().Select(m => new SelectOption(m.ToString(), m.ToString()))],
                onValueChange: async v => { _imageModel.Value = Enum.Parse<ImageGeneratorModel>(v); }));

            view.Box(["flex-1"], content: v => v.Select(
                label: "Depth model",
                value: _depthModel.Value.ToString(),
                disabled: IsGenerating,
                triggerStyle: ["w-full"],
                options: [.. Enum.GetValues<DepthEstimatorModel>().Select(m => new SelectOption(m.ToString(), m.ToString()))],
                onValueChange: async v => { _depthModel.Value = Enum.Parse<DepthEstimatorModel>(v); }));
        });
    }

    private void RenderPromptRow(IView view)
    {
        view.Row(["gap-3 items-stretch w-full"], content: view =>
        {
            view.TextArea(
                bind: _prompt,
                style: ["flex-1 bg-white/5 border border-white/10 rounded-xl text-base px-5 py-4 leading-relaxed"],
                placeholder: "Describe an image to generate...",
                rows: 3,
                autoResize: true);

            view.Button([
                "shrink-0 self-stretch px-8 rounded-xl text-lg font-bold text-white",
                "flex items-center justify-center gap-2.5",
                "bg-[linear-gradient(90deg,#0891b2,#7c3aed)] hover:brightness-110",
                "active:motion-[0:scale-100,40:scale-[0.9],70:scale-[1.04],100:scale-100] active:motion-duration-300ms active:motion-ease-ease-out",
                "transition-[filter] disabled:opacity-50 disabled:pointer-events-none"
            ],
            disabled: IsGenerating || string.IsNullOrWhiteSpace(_prompt.Value),
            onClick: GenerateAsync,
            content: v =>
            {
                if (IsGenerating)
                {
                    v.Box([Icon.Spinner, "shrink-0"]);
                }
                else
                {
                    v.Icon([Icon.Default, "shrink-0"], name: "sparkles");
                }

                v.Text(["leading-none"], text: IsGenerating ? "Generating..." : "Generate");
            });
        });
    }

    private void RenderStatus(IView view)
    {
        if (_stage.Value == Stage.Error)
        {
            view.Text(["text-center text-red-400 text-sm"], _error.Value ?? "Something went wrong");
        }
    }

    private void RenderResult(IView view)
    {
        if (_stage.Value != Stage.Ready || _imageDataUri.Value == null || _depthDataUri.Value == null)
        {
            return;
        }

        view.Column(["w-full flex flex-col gap-4 mt-2"], content: view =>
        {
            view.Row(["gap-4 items-end flex-wrap justify-between"], content: view =>
            {
                view.Box(["min-w-[210px]"], content: v => v.Select(
                    label: "Parallax algorithm",
                    value: _algorithm.Value.ToString(),
                    triggerStyle: ["w-full"],
                    options: [.. Enum.GetValues<ParallaxAlgorithm>().Select(a => new SelectOption(a.ToString(), a.ToString()))],
                    onValueChange: async v => { _algorithm.Value = Enum.Parse<ParallaxAlgorithm>(v); }));

                view.Row(["gap-2 items-center pb-2"], content: view =>
                {
                    view.Switch([Switch.Root],
                        isChecked: _showDepth.Value,
                        onCheckedChange: async c => { _showDepth.Value = c; },
                        content: v => v.SwitchThumb([Switch.Thumb]));
                    view.Text(["text-white/70 text-sm"], "Show depth map");
                });
            });

            view.Box([
                "w-full aspect-[16/9] rounded-2xl overflow-hidden border border-white/10 cursor-crosshair",
                "shadow-[0_20px_60px_rgba(0,0,0,0.5)]"
            ], content: view =>
            {
                view.ShadertoyCanvas(
                    ["w-full h-full"],
                    shaderSource: DepthParallaxShader.Source,
                    fps: 60,
                    enableMouse: true,
                    channels: [_imageDataUri.Value!, _depthDataUri.Value!],
                    uniforms: new Dictionary<string, ShaderUniform>
                    {
                        ["uParallaxStrength"] = ShaderUniform.Float(_parallaxStrength.Value),
                        ["uAlgorithm"] = ShaderUniform.Int((int)_algorithm.Value),
                        ["uSteps"] = ShaderUniform.Int(_steps.Value),
                        ["uShowDepth"] = ShaderUniform.Int(_showDepth.Value ? 1 : 0)
                    });
            });
        });
    }
}
