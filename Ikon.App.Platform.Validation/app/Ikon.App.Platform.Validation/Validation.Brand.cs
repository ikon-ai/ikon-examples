public partial class Validation
{
    private readonly Reactive<string> _colorPrompt = new("A warm, earthy SaaS dashboard theme inspired by terracotta and desert landscapes");
    private readonly Reactive<bool> _colorPromptGenerating = new(false);
    private readonly Reactive<string?> _colorPromptError = new(null);

    private readonly Reactive<string>            _brandHex     = new("#dd2590");
    private readonly Reactive<string?>           _brandError   = new(null);
    private readonly Reactive<BrandColorScale?>  _brandScale   = new(null);

    private readonly Reactive<string>            _neutralHex   = new("#667085");
    private readonly Reactive<string?>           _neutralError = new(null);
    private readonly Reactive<BrandColorScale?>  _neutralScale = new(null);

    private readonly Reactive<string>            _errorHex     = new("#d92d20");
    private readonly Reactive<string?>           _errorError   = new(null);
    private readonly Reactive<BrandColorScale?>  _errorScale   = new(null);

    private readonly Reactive<string>            _successHex   = new("#039855");
    private readonly Reactive<string?>           _successError = new(null);
    private readonly Reactive<BrandColorScale?>  _successScale = new(null);

    private readonly Reactive<string>            _warningHex   = new("#f79009");
    private readonly Reactive<string?>           _warningError = new(null);
    private readonly Reactive<BrandColorScale?>  _warningScale = new(null);

    private readonly Reactive<string>            _infoHex      = new("#0284c7");
    private readonly Reactive<string?>           _infoError    = new(null);
    private readonly Reactive<BrandColorScale?>  _infoScale    = new(null);

    private readonly Reactive<string>            _accentHex    = new("#d97706");
    private readonly Reactive<string?>           _accentError  = new(null);
    private readonly Reactive<BrandColorScale?>  _accentScale  = new(null);

    internal void AddColorScaleCss()
    {
        var css = BuildCssVarBlock();
        if (string.IsNullOrEmpty(css)) return;
        UI.AddGlobalCss(css);
    }

    private string BuildCssVarBlock()
    {
        var sb = new System.Text.StringBuilder(":root{");
        AppendScaleVars(sb, "brand",   _brandScale.Value);
        AppendScaleVars(sb, "neutral", _neutralScale.Value);
        AppendScaleVars(sb, "error",   _errorScale.Value);
        AppendScaleVars(sb, "success", _successScale.Value);
        AppendScaleVars(sb, "warning", _warningScale.Value);
        AppendScaleVars(sb, "info",    _infoScale.Value);
        AppendScaleVars(sb, "accent", _accentScale.Value);
        sb.Append('}');
        // Return empty string if no scales are set (nothing to override)
        return sb.Length == ":root{}".Length ? "" : sb.ToString();
    }

    private static void AppendScaleVars(System.Text.StringBuilder sb, string prefix, BrandColorScale? scale)
    {
        if (scale == null) return;
        sb.Append($"--{prefix}-25:{scale.Step25};");
        sb.Append($"--{prefix}-50:{scale.Step50};");
        sb.Append($"--{prefix}-100:{scale.Step100};");
        sb.Append($"--{prefix}-200:{scale.Step200};");
        sb.Append($"--{prefix}-300:{scale.Step300};");
        sb.Append($"--{prefix}-400:{scale.Step400};");
        sb.Append($"--{prefix}-500:{scale.Step500};");
        sb.Append($"--{prefix}-600:{scale.Step600};");
        sb.Append($"--{prefix}-700:{scale.Step700};");
        sb.Append($"--{prefix}-800:{scale.Step800};");
        sb.Append($"--{prefix}-900:{scale.Step900};");
        sb.Append($"--{prefix}-950:{scale.Step950};");
    }

    private void GenerateScale(
        string hexColor,
        Reactive<string?> error,
        Reactive<BrandColorScale?> scale,
        bool neutral = false)
    {
        error.Value = null;
        try
        {
            scale.Value = ColorScaleGenerator.Generate(hexColor, neutral);
        }
        catch (Exception ex)
        {
            error.Value = ex.Message;
        }
    }

    private async Task GenerateColorSchemeFromPromptAsync()
    {
        if (string.IsNullOrWhiteSpace(_colorPrompt.Value)) return;

        _colorPromptGenerating.Value = true;
        _colorPromptError.Value = null;

        try
        {
            ColorSchemeResult? result = null;
            await foreach (var ev in Emerge.Run<ColorSchemeResult>(LLMModel.Claude45Sonnet, new KernelContext(), pass =>
            {
                pass.Command = $"""
                    Based on the following description, generate a cohesive color scheme for a UI design system.
                    Return exactly 7 hex color values — one seed color for each group.

                    Description: {_colorPrompt.Value}

                    Rules:
                    - Brand: the primary accent color that represents the theme identity
                    - Neutral: a gray tone for backgrounds, borders, and text (warm, cool, or pure gray)
                    - Error: a red-ish color for errors and destructive actions
                    - Success: a green-ish color for success states
                    - Warning: an amber/orange-ish color for warnings
                    - Info: a blue-ish color for informational elements
                    - Accent: a highlight color for text selection and focus rings, distinct from brand
                    - All values must be valid 6-digit hex colors (e.g. #7e56d8)
                    - The brand and neutral should strongly reflect the description
                    - Error/success/warning/info should complement the brand while staying recognizable
                    - Accent should complement the brand while being visually distinct
                    """;
                pass.MaxOutputTokens = 200;
            }))
            {
                if (ev is Completed<ColorSchemeResult> completed)
                    result = completed.Result;
            }

            if (result == null) return;

            _brandHex.Value = result.Brand;
            _neutralHex.Value = result.Neutral;
            _errorHex.Value = result.Error;
            _successHex.Value = result.Success;
            _warningHex.Value = result.Warning;
            _infoHex.Value = result.Info;
            _accentHex.Value = result.Accent;

            GenerateScale(_brandHex.Value, _brandError, _brandScale);
            GenerateScale(_neutralHex.Value, _neutralError, _neutralScale, neutral: true);
            GenerateScale(_errorHex.Value, _errorError, _errorScale);
            GenerateScale(_successHex.Value, _successError, _successScale);
            GenerateScale(_warningHex.Value, _warningError, _warningScale);
            GenerateScale(_infoHex.Value, _infoError, _infoScale);
            GenerateScale(_accentHex.Value, _accentError, _accentScale);
        }
        catch (Exception ex)
        {
            _colorPromptError.Value = ex.Message;
        }
        finally
        {
            _colorPromptGenerating.Value = false;
        }
    }

    private void RenderBrandSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Dynamic Brand Colours");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Generate from Prompt");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextArea([Textarea.Default],
                        placeholder: "Describe the color scheme you want...",
                        rows: 2,
                        value: _colorPrompt.Value,
                        onValueChange: async v => _colorPrompt.Value = v ?? "");

                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        if (_colorPromptGenerating.Value)
                        {
                            view.Button([Button.PrimaryMd], text: "Generating...", disabled: true);
                            view.Box([Icon.Spinner]);
                        }
                        else
                        {
                            view.Button([Button.PrimaryMd], text: "Generate Color Scheme",
                                disabled: string.IsNullOrWhiteSpace(_colorPrompt.Value),
                                onClick: async () => await GenerateColorSchemeFromPromptAsync());
                        }
                    });

                    if (!string.IsNullOrEmpty(_colorPromptError.Value))
                        view.Text(["text-sm text-error-primary"], _colorPromptError.Value!);
                });
            });

            RenderColorGroupCard(view, "Brand",   _brandHex,   _brandError,   _brandScale);
            RenderColorGroupCard(view, "Neutral", _neutralHex, _neutralError, _neutralScale, neutral: true);
            RenderColorGroupCard(view, "Error",   _errorHex,   _errorError,   _errorScale);
            RenderColorGroupCard(view, "Success", _successHex, _successError, _successScale);
            RenderColorGroupCard(view, "Warning", _warningHex, _warningError, _warningScale);
            RenderColorGroupCard(view, "Info",    _infoHex,    _infoError,    _infoScale);
            RenderColorGroupCard(view, "Accent", _accentHex,  _accentError,  _accentScale);

            // Live preview is only shown after at least one scale has been generated
            var anyGenerated = _brandScale.Value != null || _neutralScale.Value != null
                || _errorScale.Value != null || _successScale.Value != null
                || _warningScale.Value != null || _infoScale.Value != null
                || _accentScale.Value != null;

            if (anyGenerated)
                RenderBrandLivePreview(view);
        });
    }

    private void RenderColorGroupCard(
        UIView view,
        string label,
        Reactive<string> hex,
        Reactive<string?> error,
        Reactive<BrandColorScale?> scale,
        bool neutral = false)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], label);

            view.Row(["gap-3 mb-4 items-end flex-wrap"], content: view =>
            {
                view.TextField(
                    [Input.Default, "w-40"],
                    placeholder: "#7e56d8",
                    value: hex.Value,
                    onValueChange: async v => { hex.Value = v; });

                view.Button([Button.PrimaryMd], text: "Generate",
                    onClick: async () => GenerateScale(hex.Value, error, scale, neutral));
            });

            if (!string.IsNullOrEmpty(error.Value))
                view.Text(["text-sm", "text-error-primary"], error.Value!);

            if (scale.Value is { } s)
            {
                view.Row(["gap-1 flex-nowrap w-full"], content: view =>
                {
                    RenderSwatch(view, "25",  s.Step25);
                    RenderSwatch(view, "50",  s.Step50);
                    RenderSwatch(view, "100", s.Step100);
                    RenderSwatch(view, "200", s.Step200);
                    RenderSwatch(view, "300", s.Step300);
                    RenderSwatch(view, "400", s.Step400);
                    RenderSwatch(view, "500", s.Step500);
                    RenderSwatch(view, "600", s.Step600);
                    RenderSwatch(view, "700", s.Step700);
                    RenderSwatch(view, "800", s.Step800);
                    RenderSwatch(view, "900", s.Step900);
                    RenderSwatch(view, "950", s.Step950);
                });
            }
        });
    }

    private static void RenderSwatch(UIView view, string step, string hex)
    {
        view.Column(["items-center gap-0.5 flex-1 min-w-0"], content: view =>
        {
            view.Box([$"w-full h-9 rounded", $"bg-[{hex}]"]);
            view.Text([Text.Body], step);
            view.Text([Text.Body, "text-[12px] text-tertiary"], hex);
        });
    }

    private void RenderBrandLivePreview(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.H3, "mb-4"], "Live Preview");

            view.Column([Layout.Column.Xl], content: view =>
            {
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Buttons");
                    view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                    {
                        view.Button([Button.PrimaryMd], text: "Primary");
                        view.Button([Button.NeutralMd], text: "Secondary");
                        view.Button([Button.OutlineMd], text: "Outline");
                        view.Button([Button.GhostMd], text: "Ghost");
                    });
                });

                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Text tokens");
                    view.Column([Layout.Column.Xs], content: view =>
                    {
                        view.Text(["text-brand-primary"], "text-brand-primary");
                        view.Text(["text-brand-secondary"], "text-brand-secondary");
                        view.Text(["text-brand-tertiary"], "text-brand-tertiary");
                        view.Text(["text-error-primary"], "text-error-primary");
                        view.Text(["text-success-primary"], "text-success-primary");
                        view.Text(["text-warning-primary"], "text-warning-primary");
                    });
                });

                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Label], "Background tokens");
                    view.Row(["gap-2 flex-wrap"], content: view =>
                    {
                        RenderSemanticSwatch(view, "bg-brand-solid",    "brand-solid");
                        RenderSemanticSwatch(view, "bg-brand-primary",  "brand-primary");
                        RenderSemanticSwatch(view, "bg-error-solid",    "error-solid");
                        RenderSemanticSwatch(view, "bg-success-solid",  "success-solid");
                        RenderSemanticSwatch(view, "bg-warning-solid",  "warning-solid");
                    });
                });
            });
        });
    }

    private static void RenderSemanticSwatch(UIView view, string bgClass, string label)
    {
        view.Column(["items-center gap-1"], content: view =>
        {
            view.Box([$"w-12 h-12 rounded border border-secondary", bgClass]);
            view.Text([Text.Caption], label);
        });
    }
}

internal sealed class BrandColorScale(
    string step25, string step50, string step100, string step200, string step300, string step400,
    string step500, string step600, string step700, string step800, string step900, string step950)
{
    public string Step25  { get; } = step25;
    public string Step50  { get; } = step50;
    public string Step100 { get; } = step100;
    public string Step200 { get; } = step200;
    public string Step300 { get; } = step300;
    public string Step400 { get; } = step400;
    public string Step500 { get; } = step500;
    public string Step600 { get; } = step600;
    public string Step700 { get; } = step700;
    public string Step800 { get; } = step800;
    public string Step900 { get; } = step900;
    public string Step950 { get; } = step950;
}

// Generates a 12-step colour scale from a seed hex using OKLCH.
// The seed maps to step 600. Lightness is varied while chroma and hue are preserved.
internal static class ColorScaleGenerator
{
    // Neutral greys — steep S-curve reaching near-black at 950 for maximum contrast range.
    private static readonly (string Name, double L)[] NeutralSteps =
    [
        ("25",  0.991),
        ("50",  0.978),
        ("100", 0.959),
        ("200", 0.921),
        ("300", 0.852),
        ("400", 0.732),
        ("500", 0.587),
        ("600", 0.454),
        ("700", 0.347),
        ("800", 0.259),
        ("900", 0.190),
        ("950", 0.165),
    ];

    // Chromatic colours (brand, error, success, warning, info) — gentler curve that
    // stays lighter in the dark end to preserve colour vibrancy and avoid muddiness.
    private static readonly (string Name, double L)[] ChromaticSteps =
    [
        ("25",  0.989),
        ("50",  0.976),
        ("100", 0.956),
        ("200", 0.912),
        ("300", 0.844),
        ("400", 0.740),
        ("500", 0.648),
        ("600", 0.558),
        ("700", 0.478),
        ("800", 0.402),
        ("900", 0.338),
        ("950", 0.294),
    ];

    public static BrandColorScale Generate(string hex, bool neutral = false)
    {
        var steps = neutral ? NeutralSteps : ChromaticSteps;
        var (_, c, h) = HexToOklch(hex);
        var hexSteps = new string[12];

        for (int i = 0; i < steps.Length; i++)
        {
            double dist = steps[i].L - 0.55;
            double chromaScale = dist >= 0
                ? 1.0 - Math.Pow(dist / 0.45, 1.5) * 0.976
                : 1.0 - Math.Pow(-dist / 0.30, 2.0) * 0.642;
            hexSteps[i] = OklchToHex(steps[i].L, c * Math.Max(chromaScale, 0.0), h);
        }

        return new BrandColorScale(
            hexSteps[0], hexSteps[1], hexSteps[2], hexSteps[3], hexSteps[4], hexSteps[5],
            hexSteps[6], hexSteps[7], hexSteps[8], hexSteps[9], hexSteps[10], hexSteps[11]);
    }

    private static (double L, double C, double H) HexToOklch(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length != 6)
        {
            throw new ArgumentException($"Invalid hex colour: #{hex}");
        }

        double r = int.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber) / 255.0;
        double g = int.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber) / 255.0;
        double b = int.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber) / 255.0;

        // sRGB → linear
        r = SrgbToLinear(r);
        g = SrgbToLinear(g);
        b = SrgbToLinear(b);

        // Linear RGB → OKLab
        double l_ = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
        double m_ = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
        double s_ = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

        l_ = Math.Cbrt(l_);
        m_ = Math.Cbrt(m_);
        s_ = Math.Cbrt(s_);

        double labL = 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_;
        double labA = 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_;
        double labB = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_;

        // OKLab → OKLCH
        double c = Math.Sqrt(labA * labA + labB * labB);
        double h = Math.Atan2(labB, labA) * (180.0 / Math.PI);

        if (h < 0) h += 360.0;

        return (labL, c, h);
    }

    private static string OklchToHex(double l, double c, double h)
    {
        double hRad = h * (Math.PI / 180.0);
        double labA = c * Math.Cos(hRad);
        double labB = c * Math.Sin(hRad);

        // OKLab → LMS
        double l_ = l + 0.3963377774 * labA + 0.2158037573 * labB;
        double m_ = l - 0.1055613458 * labA - 0.0638541728 * labB;
        double s_ = l - 0.0894841775 * labA - 1.2914855480 * labB;

        l_ = l_ * l_ * l_;
        m_ = m_ * m_ * m_;
        s_ = s_ * s_ * s_;

        // LMS → linear RGB
        double r = +4.0767416621 * l_ - 3.3077115913 * m_ + 0.2309699292 * s_;
        double g = -1.2684380046 * l_ + 2.6097574011 * m_ - 0.3413193965 * s_;
        double b = -0.0041960863 * l_ - 0.7034186147 * m_ + 1.7076147010 * s_;

        // Linear → sRGB, clamp
        int ri = Math.Clamp((int)Math.Round(LinearToSrgb(r) * 255), 0, 255);
        int gi = Math.Clamp((int)Math.Round(LinearToSrgb(g) * 255), 0, 255);
        int bi = Math.Clamp((int)Math.Round(LinearToSrgb(b) * 255), 0, 255);

        return $"#{ri:x2}{gi:x2}{bi:x2}";
    }

    private static double SrgbToLinear(double v) =>
        v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);

    private static double LinearToSrgb(double v) =>
        v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
}

internal sealed class ColorSchemeResult
{
    public string Brand { get; set; } = "#7e56d8";
    public string Neutral { get; set; } = "#667085";
    public string Error { get; set; } = "#d92d20";
    public string Success { get; set; } = "#039855";
    public string Warning { get; set; } = "#f79009";
    public string Info { get; set; } = "#0284c7";
    public string Accent { get; set; } = "#d97706";
}
