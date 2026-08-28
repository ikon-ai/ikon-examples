// Ikon's own identity, per brand/ikon-brand.md, in dark: monochrome charcoal with one electric
// magenta accent, and hi-vis yellow held back for what the guide reserves it for — live signals and
// reward. Colour comes from the content, which here is the route line and the medals.
public static class Brand
{
    public const string Page = "#0b0b0d";
    public const string Surface = "#161618";
    public const string Card = "#1c1c1f";
    public const string Active = "#242427";
    public const string Quaternary = "#2f2f33";
    public const string Line = "#2f2f33";

    public const string Magenta = "#db176e";
    public const string MagentaBright = "#f5277f";
    public const string MagentaText = "#e62e7d";
    public const string MagentaLight = "#ff5ba0";

    // The reward gold. Gold medals and the recording indicator, and nothing else.
    public const string Gold = "#f2da00";
    public const string Teal = "#38d3bd";

    public const string TextPrimary = "#f7f7f7";
    public const string TextSecondary = "#d3d5d9";
    public const string TextTertiary = "#aeb2b8";
    public const string TextQuaternary = "#9ea2a9";

    // Fixed dark: this is a consumer app with a night-legible map at its centre, and a client in
    // light mode must not pull light text defaults in under these charcoal surfaces.
    public static IkonTheme Theme => new()
    {
        Mode = ThemeMode.Fixed,
        ["primary"] = Magenta,
        ["primary-foreground"] = "#ffffff",
        ["bg-brand-solid"] = Magenta,
        ["bg-brand-solid-hover"] = MagentaBright,
        ["bg-brand-button"] = Magenta,
        ["bg-brand-button-hover"] = MagentaBright,
        ["background"] = Page,
        ["foreground"] = TextSecondary,
        ["card"] = Card,
        ["popover"] = Card,
        ["bg-secondary"] = Surface,
        ["bg-accent"] = Active,
        ["muted"] = Surface,
        ["muted-foreground"] = TextQuaternary,
        ["border"] = Line,
        ["input"] = Active,
        ["ring"] = MagentaBright,
        ["radius"] = "0.25rem",
        ["font-heading"] = "Poppins",
        ["font-body"] = "Inter",
        ["font-mono"] = "JetBrains Mono",
        ["motion-duration-base"] = "160ms",
        ["ease-default"] = "ease-out",
    };

    // Small uppercase mono label above a number or a section. Display type is tight and heavy, micro
    // type is wide and tracked — never the reverse.
    public const string Kicker = "font-mono text-[10px] uppercase tracking-[0.22em] text-muted-foreground";

    public const string KickerBrand = "font-mono text-[10px] uppercase tracking-[0.22em] text-[#e62e7d]";

    // The one huge number a screen is built around.
    public const string Hero = "font-heading font-semibold tabular-nums tracking-[-0.035em] leading-[0.9] text-[#f7f7f7]";

    public const string Numeral = "font-heading font-semibold tabular-nums tracking-[-0.02em] leading-none text-[#f7f7f7]";

    public const string Mono = "font-mono tabular-nums text-[#d3d5d9]";

    public const string Title = "font-heading font-semibold tracking-[-0.02em] text-[#f7f7f7]";

    // A ring rather than a border: that is the depth idiom, and shadows stay flat.
    public const string Panel = "bg-card rounded-lg ring-1 ring-white/[0.06]";

    public static readonly string[] ChartColors = [Magenta, Teal, Gold, MagentaLight];

    public static string MedalHex(MedalTier tier) => tier switch
    {
        MedalTier.Gold => Gold,
        MedalTier.Silver => TextTertiary,
        MedalTier.Bronze => "#b07a45",
        _ => Quaternary,
    };

    public static string MedalLabel(MedalTier tier) => tier switch
    {
        MedalTier.Gold => "Gold",
        MedalTier.Silver => "Silver",
        MedalTier.Bronze => "Bronze",
        _ => "",
    };
}
