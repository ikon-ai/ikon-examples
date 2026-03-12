# Styling & Motion

## Styling

Crosswind uses Tailwind-like syntax in style arrays. Mix theme constants with custom classes:

```csharp
view.Button([Button.PrimaryMd, "mt-2 w-fit self-center"], label: "Submit");
view.Box([Card.Default, "p-6 mb-4"], content: view => { ... });
```

### Theme Constants

From `Ikon.Parallax.Themes.Default`:

- **Page**: `Page.Default`
- **Container**: `Container.Xl2`, `Container.Xl4`
- **Button**: `Button.PrimaryMd`, `Button.SecondaryMd`, `Button.OutlineMd`, `Button.GhostMd`, `Button.DangerMd`, `Button.Size.Icon`
- **Input**: `Input.Default`
- **Text**: `Text.Display`, `Text.H2`, `Text.H3`, `Text.Body`, `Text.BodyStrong`, `Text.Caption`, `Text.Muted`, `Text.Small`, `Text.Code`, `Text.Label`, `Text.Link`
- **Card**: `Card.Default`, `Card.Elevated`
- **Layout**: `Layout.Row.Sm/Md/Lg`, `Layout.Row.SpaceBetween`, `Layout.Column.Xs/Sm/Md/Lg`, `Layout.Center`
- **Icon**: `Icon.Default`, `Icon.Spinner`
- **FormField**: `FormField.Root`, `FormField.Label`
- **Overlay styles**: `Dialog.*`, `AlertDialog.*`, `Popover.*`, `Tooltip.*`, `HoverCard.*`, `Toast.*`, `Sheet.*`, `Drawer.*`
- **Navigation**: `Tabs.List`, `Tabs.Trigger`, `Tabs.Content`, `Slider.*`, `Select.*`, `RadioGroup.*`, `Checkbox.*`, `Switch.*`
- **Tokens**: `Tokens.Radius.Lg`, `Tokens.Width.Toast`

### Motion Syntax

```csharp
// Per-letter wave animation
view.Text(["wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] wave:motion-duration-2500ms wave:motion-per-letter wave:motion-loop"], "Hello");
```

Refer to the Crosswind Styling and Motion Guide in generated docs for full details.

### Style Organization

For large apps, organize styles in a static class:

```csharp
internal static class Styles
{
    public static readonly string[] PageContainer = [Container.Xl2, "py-8 px-4 min-h-screen"];
    public static readonly string[] MainCard = [Card.Default, Layout.Column.Lg, "p-10 w-full"];
}
```

---

# Ikon.Crosswind Public API

namespace Ikon.Crosswind
  enum ArgType
    Length
    Color
    Time
    Angle
    Fraction
    Keyword
    Unknown
  class ArgValue : IEquatable<ArgValue>
    ctor(string Raw, string Value, Unit Unit, ArgType Type)
    string Raw { get;  init; }
    ArgType Type { get;  init; }
    Unit Unit { get;  init; }
    string Value { get;  init; }
    bool IsColor()
    bool IsKeyword()
    bool IsLength()
    bool IsUnit()
    bool IsUnknown()
  sealed class CanvasDesignTokenDocument
    ctor()
    Dictionary<string, CanvasTokenValue<string>> BackdropBlur { get;  init; }
    Dictionary<string, Dictionary<string, CanvasTokenValue<string>>> ColorScales { get;  init; }
    CanvasEffectTokens Effects { get;  init; }
    List<string> Guidelines { get;  init; }
    Dictionary<string, string> Metadata { get;  init; }
    string Name { get;  init; }
    Dictionary<string, CanvasTokenValue<string>> Radii { get;  init; }
    CanvasSemanticColorTokens SemanticColors { get;  init; }
    CanvasTypographyTokens Typography { get;  init; }
    string Version { get;  init; }
    void Validate()
  static class CanvasDesignTokenLoader
    static CanvasDesignTokenDocument Load(Stream stream)
    static CanvasDesignTokenDocument Load(string json)
    static CanvasDesignTokenDocument LoadFromFile(string path)
  sealed class CanvasEffectTokens
    ctor()
    CanvasModeTokenSet BoxShadows { get;  init; }
    CanvasModeTokenSet FocusRing { get;  init; }
    CanvasModeTokenSet ShadowPalette { get;  init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasModeTokenSet
    ctor()
    Dictionary<string, CanvasTokenValue<string>> Dark { get;  init; }
    Dictionary<string, CanvasTokenValue<string>> Light { get;  init; }
    void Validate(string category)
  sealed class CanvasSemanticColorTokens
    ctor()
    CanvasModeTokenSet Background { get;  init; }
    CanvasModeTokenSet Border { get;  init; }
    CanvasModeTokenSet Foreground { get;  init; }
    CanvasModeTokenSet Text { get;  init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasTokenValue<T>
    ctor()
    string Description { get;  init; }
    T Value { get;  init; }
  sealed class CanvasTypographyScale
    ctor()
    string Description { get;  init; }
    string FontFamily { get;  init; }
    string FontSize { get;  init; }
    string LetterSpacing { get;  init; }
    string LineHeight { get;  init; }
    void Validate(string tokenName)
  sealed class CanvasTypographyTokens
    ctor()
    Dictionary<string, CanvasTypographyScale> Display { get;  init; }
    Dictionary<string, CanvasTokenValue<string>> FontFamilies { get;  init; }
    Dictionary<string, CanvasTokenValue<int>> FontWeights { get;  init; }
    Dictionary<string, CanvasTypographyScale> Text { get;  init; }
    void Validate()
  class CompileResult : IEquatable<CompileResult>
    ctor(List<CssRule> Rules, List<string> ExtraCss, List<MotionBindingMetadata> MotionBindings)
    List<string> ExtraCss { get;  init; }
    List<MotionBindingMetadata> MotionBindings { get;  init; }
    List<CssRule> Rules { get;  init; }
  sealed class CompiledStyle : IEquatable<CompiledStyle>
    ctor(string Css, IReadOnlyList<MotionBindingMetadata> MotionBindings)
    string Css { get;  init; }
    IReadOnlyList<MotionBindingMetadata> MotionBindings { get;  init; }
  class ContainerVariant : IEquatable<ContainerVariant>
    ctor(string Name, string Query, string Breakpoint)
    string Breakpoint { get;  init; }
    string Name { get;  init; }
    string Query { get;  init; }
    bool WantsBreakpoint { get; }
    ContainerVariant WithBreakpoint(string breakpoint)
  static class CssEmitter
    static string Emit(CompileResult result)
  static class CssProcessor
    static string GetCss(string tailwindDeclaration, string classId)
    static CompiledStyle GetStyle(string tailwindDeclaration, string classId)
  class CssRule : IEquatable<CssRule>
    ctor(string AtRule, string Selector, Dictionary<string, string> Decls)
    string AtRule { get;  init; }
    Dictionary<string, string> Decls { get;  init; }
    string Selector { get;  init; }
  enum DarkModeStrategy
    Media
    Class
  sealed class MotionBindingMetadata : IEquatable<MotionBindingMetadata>
    ctor(string Source, string Min, string Max, string Clamp, bool Reverse, string Ease, string Map, string TargetId)
    string Clamp { get;  init; }
    string Ease { get;  init; }
    string Map { get;  init; }
    string Max { get;  init; }
    string Min { get;  init; }
    bool Reverse { get;  init; }
    string Source { get;  init; }
    string TargetId { get;  init; }
  static class SelectorComposer
    static IReadOnlyDictionary<string, string> BreakpointMap { get; }
    static string DarkClassSelector { get;  set; }
    static DarkModeStrategy DarkMode { get;  set; }
    static bool EnableThemeVariant { get;  set; }
    static string GroupClassSelector { get;  set; }
    static string PeerClassSelector { get;  set; }
    static ThemeSelectorStrategy ThemeStrategy { get;  set; }
    static string Compose(string baseSelector, IReadOnlyList<string> variants, string track, ContainerVariant container = null)
    static ValueTuple<string, string> ComposeTemplate(IReadOnlyList<string> variants, string track, ContainerVariant container = null)
  static class TW
    static string FormatLength(ArgValue a)
    static string FractionToPercent(string frac)
    static string MaybeNegate(bool negative, string val)
    static string ResolveColor(string raw, TailwindColorContext context = Generic)
    static string ResolveFontFamily(string token)
    static string ResolveFontWeight(string token)
    static string ResolveLetterSpacing(string tokenOrLength)
    static string ResolveLineHeight(string tokenOrLength)
    static string ResolveOpacity(string token)
    static string ResolveRadius(string tokenOrLength)
    static string ResolveShadow(string token)
    static string ResolveTextAlign(string token)
    static ValueTuple<string, string> ResolveTextSize(string tokenOrLength)
    static string SpacingTokenToLength(string token)
    static string UnitToSuffix(Unit u)
  enum TailwindColorContext
    Generic
    Background
    Foreground
    Text
    Border
  sealed class TailwindColorDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string> background, IReadOnlyDictionary<string, string> foreground, IReadOnlyDictionary<string, string> text, IReadOnlyDictionary<string, string> border)
    IReadOnlyDictionary<string, string> Background { get;  init; }
    IReadOnlyDictionary<string, string> Border { get;  init; }
    IReadOnlyDictionary<string, string> Foreground { get;  init; }
    IReadOnlyDictionary<string, string> Text { get;  init; }
    void Validate()
  static class TailwindCompiler
    static CompileResult CompileRules(string baseSelector, string classAttr)
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    static string GetFullBaseline()
  sealed class TailwindCssVariables
    ctor(IDictionary<string, string> light, IDictionary<string, string> dark, string darkThemeName = "dark")
    IReadOnlyDictionary<string, string> Dark { get; }
    string DarkThemeName { get; }
    IReadOnlyDictionary<string, string> Light { get; }
    string EmitDark()
    string EmitLight()
  static class TailwindCustomStyleRegistry
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    static void MergeDefinitions(TailwindStyleDefinitions definitions)
    static void SetDefinitions(TailwindStyleDefinitions definitions)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  static class TailwindDedup
    static List<TailwindDescription> Deduplicate(List<TailwindDescription> classes)
  class TailwindDescription : IEquatable<TailwindDescription>
    ctor(List<string> Variants, string Track, string Utility, List<ArgValue> Args, bool Important, bool Negative, ContainerVariant Container = null, bool HasBracketArg = false)
    List<ArgValue> Args { get;  init; }
    ContainerVariant Container { get;  init; }
    bool HasBracketArg { get;  init; }
    bool Important { get;  init; }
    bool Negative { get;  init; }
    string Track { get;  init; }
    string TrackKey { get; }
    string Utility { get;  init; }
    List<string> Variants { get;  init; }
  sealed class TailwindDesignTokenResult
    ctor(TailwindCssVariables cssVariables, TailwindThemeDefinition theme, TailwindStyleDefinitions styleDefinitions)
    TailwindCssVariables CssVariables { get; }
    TailwindStyleDefinitions StyleDefinitions { get; }
    TailwindThemeDefinition Theme { get; }
  sealed class TailwindFontDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string> family, IReadOnlyDictionary<string, string> weight)
    IReadOnlyDictionary<string, string> Family { get;  init; }
    IReadOnlyDictionary<string, string> Weight { get;  init; }
    void Validate()
  sealed class TailwindFontSize
    ctor(string size, string lineHeight, string letterSpacing)
    string LetterSpacing { get; }
    string LineHeight { get; }
    string Size { get; }
  static class TailwindNormalizer
    static TailwindDescription Normalize(TailwindDescription tw)
  static class TailwindParser
    static List<TailwindDescription> ParseManyRaw(string inputLine)
    static TailwindDescription ParseRaw(string input)
    static ValueTuple<List<string>, string, ContainerVariant> SplitVariants(List<string> variants)
  static class TailwindStyleDefinitionLoader
    static Task<TailwindStyleDefinitions> Load(AssetUri assetUri)
    static TailwindStyleDefinitions LoadFromCss(string css)
    static TailwindStyleDefinitions LoadFromFile(string path)
    static TailwindStyleDefinitions LoadFromJson(string json)
    static TailwindStyleDefinitions LoadFromStream(Stream stream)
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions fonts = null)
    TailwindColorDefinitions Colors { get;  init; }
    TailwindFontDefinitions Fonts { get;  init; }
    void Validate()
  sealed class TailwindThemeDefinition
    ctor(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colorScales, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> colors, IReadOnlyDictionary<string, string> boxShadow, IReadOnlyDictionary<string, string> shadowPalette, IReadOnlyDictionary<string, string> focusRing, IReadOnlyDictionary<string, string> borderRadius, IReadOnlyDictionary<string, string> backdropBlur, IReadOnlyDictionary<string, string> fontFamily, IReadOnlyDictionary<string, string> fontWeight, IReadOnlyDictionary<string, TailwindFontSize> fontSize)
    IReadOnlyDictionary<string, string> BackdropBlur { get; }
    IReadOnlyDictionary<string, string> BorderRadius { get; }
    IReadOnlyDictionary<string, string> BoxShadow { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ColorScales { get; }
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; }
    IReadOnlyDictionary<string, string> FocusRing { get; }
    IReadOnlyDictionary<string, string> FontFamily { get; }
    IReadOnlyDictionary<string, TailwindFontSize> FontSize { get; }
    IReadOnlyDictionary<string, string> FontWeight { get; }
    IReadOnlyDictionary<string, string> ShadowPalette { get; }
  static class ThemeEmitter
    static string Emit(IDictionary<string, string> vars, string themeName = null)
  enum ThemeSelectorStrategy
    Attribute
    Class
  static class ThemeToTailwindConverter
    static TailwindDesignTokenResult Convert(CanvasDesignTokenDocument document)
  static class ThemeVars
    static bool VariableFallbacksEnabled { get;  set; }
    static string Var(string name, string fallback = null)
  static class TransformCombiner
    static List<TailwindDescription> Combine(List<TailwindDescription> classes)
    static Dictionary<string, string> ComposeForMotion(List<TailwindDescription> classes)
  enum Unit
    Px
    Rem
    Em
    Percent
    Vw
    Vh
    Vmin
    Vmax
    Svw
    Svh
    Lvw
    Lvh
    Dvw
    Dvh
    Cqw
    Cqh
    Cqi
    Cqb
    Cqmin
    Cqmax
    Svb
    Svi
    Lvb
    Lvi
    Dvb
    Dvi
    Svmin
    Svmax
    Lvmin
    Lvmax
    Dvmin
    Dvmax
    Ch
    Ex
    Cm
    Mm
    In
    Pt
    Pc
    Ms
    S
    Deg
    Rad
    Turn
    None
    Unknown
  static class Utilities
    static Dictionary<string, Dictionary<string, string>> Accent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AccentColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Align(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AlignSelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Animate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Antialiased(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Appearance(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Aspect(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AutoCols(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> AutoRows(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropBlur(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropBrightness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropContrast(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropFilter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropFilterNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BackdropGrayscale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropHueRotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropInvert(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropOpacity(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropSaturate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackdropSepia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackgroundBlend(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BackgroundColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Basis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgCenter(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipBorder(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipContent(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipPadding(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgClipText(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgContain(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgCover(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgGradientToB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToBl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToBr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToTl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgGradientToTr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgLeft(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLeftBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLeftTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinear(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgLinearToB(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToBl(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToBr(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToL(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToR(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToT(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToTl(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLinearToTr(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgLocal(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgNoRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginBorder(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginContent(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgOriginPadding(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatRound(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatSpace(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatX(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRepeatY(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRight(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRightBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgRightTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgScroll(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BgSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BgTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Block(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Blur(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Border(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderBColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderCollapse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BorderColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderE(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderEColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderLColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderRColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderRadius(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderS(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSeparate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> BorderSpacing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSpacingX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderSpacingY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderTColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderXColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BorderYColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Bottom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BoxDecoration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BoxSizing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Break(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakAfter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakBefore(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> BreakInside(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Brightness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Capitalize(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> CaptionBottom(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> CaptionTop(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Caret(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> CaretColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Clear(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Collapse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> ColorScheme(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Columns(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Container(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Content(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Contents(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Contrast(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Cursor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DecorationStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DecorationThickness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Delay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DiagonalFractions(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DivideColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideXReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DivideY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> DivideYReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> DropShadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Duration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ease(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> End(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FieldSizing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Fill(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Filter(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FilterNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Flex1(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexCol(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexColReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexDisplay(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexInitial(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexNoWrap(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexRow(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexRowReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexWrap(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FlexWrapReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Float(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FlowRoot(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> FontFamily(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontPalette(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontPaletteValues(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontStretch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> FontWeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ForcedColorAdjust(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Gap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GapX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GapY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientFrom(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientTo(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientVia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GradientViaNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Grayscale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridCols(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumn(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnSpan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridColumnStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridDisplay(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> GridFlow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowSpan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRowStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> GridRows(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Grow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Grow0(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Height(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Hidden(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> HueRotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Hyphens(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Indent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Inline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineBlock(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineFlex(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineGrid(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> InlineTable(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Inset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlock(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlockEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetBlockStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInlineEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetInlineStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetRing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> InsetY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Invert(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Invisible(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Isolate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> IsolationAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Italic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> JustifyContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> JustifyItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> JustifySelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Left(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LetterSpacing(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineClamp(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> LineThrough(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> LiningNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> List(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ListImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ListItem(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Lowercase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> M(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskClip(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskComposite(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskImage(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskNoRepeat(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskOrigin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskPosition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeat(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatRound(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatSpace(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatX(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskRepeatY(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MaskSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaxHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MaxWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Me(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MinHeight(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MinWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MixBlend(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ml(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Motion(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBind(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBindEase(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionBindReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> MotionComposition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionDelay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionDuration(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionEase(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionFill(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionLetterDelay(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionMap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionOnce(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerChildren(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetter(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLetterReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLine(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLinePingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerLineReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerParagraph(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWord(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordReverse(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPerWordReverseLoop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPingPong(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPlayState(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionPriority(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRange(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRangeEnd(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRangeStart(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionRate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> MotionTimeline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ms(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Mx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> My(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> NoUnderline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NormalCase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NormalNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NotItalic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> NotSrOnly(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Object(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OldstyleNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Opacity(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Order(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ordinal(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Outline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineNone(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> OutlineOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineStyle(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OutlineWidth(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Overflow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverflowY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Overline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Overscroll(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> OverscrollY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> P(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceContent(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceItems(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PlaceSelf(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PointerEvents(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> PosAbsolute(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosRelative(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosStatic(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> PosSticky(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Pr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ProportionalNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Ps(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Pt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Px(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Py(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Resize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Right(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Ring(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RingInset(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> RingOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Rotate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RotateX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RotateY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedB(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedBl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedBr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedE(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedEe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedEs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedL(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedR(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedS(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedSe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedSs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedT(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedTl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> RoundedTr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Saturate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Scale(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScaleX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScaleY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollBehavior(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollM(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollMy(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollP(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPb(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPe(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPl(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPr(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPs(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPt(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPx(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollPy(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ScrollTimeline(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Select(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Sepia(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shrink(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Shrink0(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Size(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SkewX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SkewY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SlashedZero(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SnapAlign(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapAxis(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapStop(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SnapStrictness(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceXReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SpaceY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SpaceYReverse(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> SrOnly(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> StackedFractions(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Start(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Stroke(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeDasharray(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeDashoffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> StrokeJoin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> SubpixelAntialiased(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Table(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableAuto(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableCaption(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableCell(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableColumn(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableColumnGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableFixed(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableFooterGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableHeaderGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableRow(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TableRowGroup(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TabularNums(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextAlign(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextClip(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextColor(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextEllipsis(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> TextShadow(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextSize(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TextWrap(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Top(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Touch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TouchPan(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TouchPinch(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Transform(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransformNone(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransformOrigin(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Transition(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransitionBehavior(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TransitionNone(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Translate(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateX(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateY(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> TranslateZ(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Truncate(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Underline(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> UnderlineOffset(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Uppercase(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Visible(TailwindDescription _)
    static Dictionary<string, Dictionary<string, string>> Whitespace(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> Width(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> WillChange(TailwindDescription cls)
    static Dictionary<string, Dictionary<string, string>> ZIndex(TailwindDescription cls)
  class UtilityAttribute : Attribute
    ctor(string prefix)
    string Prefix { get; }
  static class UtilityExec
    static bool HasUtility(string prefix)
    static Dictionary<string, Dictionary<string, string>> Run(TailwindDescription tw)


---

# Crosswind Styling and Motion Guide

## Overview

Crosswind is Ikon's utility-first styling and animation system. The name comes from being Tailwind-inspired while extending it with additional features, particularly a motion language for declarative animations.

The library lives in `Ikon.Crosswind` and provides:

- **Tailwind-compatible syntax**: Standard Tailwind utility classes work as expected (`flex`, `gap-4`, `bg-blue-500`, `hover:bg-blue-600`, etc.)
- **Motion language**: Extended syntax for declarative keyframe animations, staggered text effects, and input-bound timelines
- **Server-side compilation**: Classes are parsed and compiled to CSS on the server, with only the resulting styles sent to clients

## How It Works

When you pass style strings to UI components, Crosswind processes them through a compilation pipeline:

1. **Parsing**: Class strings are tokenized, handling variant chains (`hover:`, `focus:`, `data-[state=open]:`), arbitrary values (`bg-[#ff0000]`), negative prefixes (`-translate-y-1`), and importance modifiers (`!mt-4`)

2. **Normalization**: Canonical forms are resolved (e.g., `flex-grow` becomes `grow`, `content-start` becomes `content-align-start`)

3. **Deduplication**: Duplicate utilities are eliminated, and related properties (transforms, filters) are combined

4. **CSS Generation**: Final CSS rules are emitted with unique identifiers. For motion utilities, keyframe animations and `@property` rules are also generated

5. **Delivery**: The compiled CSS is sent to clients as part of the UI stream. Clients receive only the styles they need

## Usage in Applications

In Ikon AI Apps, styles are passed as string arrays to UI components. The `Ikon.Parallax` UI system handles the integration with Crosswind automatically.

```csharp
view.Button(
    style: ["px-4 py-2 rounded-lg bg-blue-500 hover:bg-blue-600 text-white transition"],
    label: "Click me",
    onClick: async () => { }
);
```

Multiple style strings can be combined:

```csharp
private const string BaseButton = "px-4 py-2 rounded-lg font-medium transition-colors";
private const string PrimaryColors = "bg-blue-500 hover:bg-blue-600 text-white";

view.Button(
    style: [BaseButton, PrimaryColors],
    label: "Primary Action",
    onClick: async () => { }
);
```

### Organizing Styles

Applications typically organize styles into static classes with constants:

```csharp
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
```

### Built-in Themes

Ikon provides built-in themes with organized style constants:

```csharp
using Ikon.Parallax.Themes.Default;

view.Button(style: [Button.PrimaryMd], label: "Submit");
view.TextField(style: [Input.Default], defaultValue: "");
view.Box(style: [Card.Default], content: view => { });
```

## Utility Classes

Crosswind supports the standard Tailwind utility classes:

| Category | Examples |
|----------|----------|
| Spacing | `p-4`, `mx-auto`, `gap-3`, `space-y-2` |
| Sizing | `w-full`, `h-screen`, `max-w-md`, `min-h-0` |
| Layout | `flex`, `grid`, `block`, `hidden`, `grid-cols-3` |
| Flexbox | `items-center`, `justify-between`, `flex-1`, `flex-wrap` |
| Typography | `text-sm`, `font-bold`, `tracking-wide`, `leading-tight` |
| Colors | `text-white`, `bg-blue-500`, `border-neutral-700` |
| Borders | `border`, `rounded-lg`, `border-2`, `divide-y` |
| Effects | `shadow-lg`, `opacity-50`, `blur-sm` |
| Transforms | `scale-105`, `rotate-45`, `-translate-y-1` |
| Transitions | `transition`, `duration-200`, `ease-in-out` |
| Interactivity | `cursor-pointer`, `select-none`, `pointer-events-none` |

### Variants

Standard Tailwind variants are supported:

```csharp
// Pseudo-classes
"hover:bg-blue-600 focus:ring-2 active:scale-95 disabled:opacity-50"

// Responsive breakpoints
"sm:flex md:grid lg:hidden"

// Dark mode
"dark:bg-neutral-900 dark:text-white"

// Data attributes
"data-[state=open]:bg-blue-500 data-[disabled]:opacity-50"

// Group and peer
"group-hover:visible peer-focus:ring-2"
```

### Arbitrary Values

Use brackets for custom values:

```csharp
// Custom colors
"bg-[#ff6b6b] text-[rgb(255,255,255)]"

// Custom spacing
"p-[13px] gap-[0.875rem]"

// Custom properties
"shadow-[0_0_20px_rgba(0,255,65,0.3)]"
```

## Motion Language

Crosswind extends Tailwind with a motion system for declarative animations. Motion utilities compile to CSS keyframe animations with proper `@property` rules for animatable custom properties.

### Keyframe Timelines

Define animations with the `motion-[...]` syntax. Steps are specified as `percentage:utilities` pairs.

Within `motion-[...]`, keyframe steps are comma-separated. Within each step, multiple utilities are separated by underscores (`_`). Underscores outside of brackets and parentheses are converted to spaces during parsing, so `opacity-0_translate-y-[12px]` is equivalent to `opacity-0 translate-y-[12px]`.

```csharp
// Fade in and slide up
"motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0]"

// Scale pulse
"motion-[0:scale-100,50:scale-[1.05],100:scale-100]"

// Complex multi-step animation
"motion-[0:opacity-0_blur-[4px],30:opacity-60_blur-[2px],100:opacity-100_blur-0]"
```

### Timing Controls

Control animation timing with dedicated utilities:

```csharp
// Duration and delay
"motion-duration-300ms motion-delay-100ms"

// Easing
"motion-ease-[cubic-bezier(0.25,1,0.35,1)]"

// Fill mode
"motion-fill-both motion-fill-forwards"

// Iteration
"motion-once motion-loop motion-ping-pong"

// Step easing (discrete/glitch effects)
"motion-ease-[steps(1)]"   // instant jumps between keyframes
"motion-ease-[steps(4)]"   // four evenly-spaced steps

// Playback rate multiplier
"motion-rate-150"           // 150% speed
```

### Staggered Text Animations

Animate text character by character, word by word, or line by line:

```csharp
// Typewriter effect - letters appear one at a time
"motion-[0:opacity-0,100:opacity-100] " +
"motion-duration-80ms motion-stagger-50ms motion-per-letter motion-fill-both"

// Words fade in sequentially
"motion-[0:opacity-0_translate-y-[8px],100:opacity-100_translate-y-0] " +
"motion-duration-200ms motion-stagger-120ms motion-per-word motion-fill-both"

// Lines reveal one by one
"motion-[0:opacity-0,100:opacity-100] " +
"motion-duration-300ms motion-stagger-200ms motion-per-line motion-fill-both"
```

#### Per-Element Modes and Compound Variants

Base modes split text (or children) into individually animated segments:

- `motion-per-letter` — each character
- `motion-per-word` — each word
- `motion-per-line` — each line (split on `\n`)
- `motion-per-paragraph` — each paragraph
- `motion-per-children` — each child element

Each base mode supports compound suffixes that combine the split with a playback modifier:

| Suffix | Effect | Example |
|--------|--------|---------|
| `-loop` | Infinite iteration | `motion-per-letter-loop` |
| `-ping-pong` | Alternate direction + infinite | `motion-per-word-ping-pong` |
| `-reverse` | Stagger from last element backward | `motion-per-line-reverse` |
| `-reverse-loop` | Reverse stagger + infinite | `motion-per-letter-reverse-loop` |

These compound variants are available for `per-letter`, `per-word`, and `per-line`.

```csharp
// Looping wave — each letter bounces continuously
"wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] " +
"wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-loop wave:motion-ease-ease-in-out"

// Reverse loop — stagger starts from the last letter
"wave:motion-[0:translate-y-0,50:translate-y-[-10px],100:translate-y-0] " +
"wave:motion-duration-1200ms wave:motion-stagger-80ms wave:motion-per-letter-reverse-loop wave:motion-ease-ease-in-out"

// Ping-pong — alternating direction per word
"motion-[0:opacity-70_scale-[0.95],100:opacity-100_scale-100] " +
"motion-duration-500ms motion-stagger-150ms motion-per-word-ping-pong"
```

### Track Prefixes

Scope motion parameters to named tracks for independent control:

```csharp
// 'title' track for text, 'glow' track for background effect
"title:motion-[0:opacity-0,100:opacity-100] title:motion-duration-300ms title:motion-per-letter " +
"glow:motion-[0:scale-100,50:scale-[1.02],100:scale-100] glow:motion-duration-2000ms glow:motion-loop"
```

### State-Based Animations

Combine motion with data attribute variants for state-driven animations:

```csharp
// Dialog content animation
"data-[state=open]:motion-[0:opacity-0_scale-[0.95],100:opacity-100_scale-100] " +
"data-[state=open]:motion-duration-200ms data-[state=open]:motion-fill-both " +
"data-[state=closed]:motion-[0:opacity-100,100:opacity-0] " +
"data-[state=closed]:motion-duration-150ms data-[state=closed]:motion-fill-both"
```

### 3D Transforms in Keyframes

Crosswind supports 3D rotation and translation utilities inside keyframe steps: `rotate-x-[angle]`, `rotate-y-[angle]`, and `translate-z-[length]`. These emit CSS custom properties with auto-registered `@property` rules so they animate smoothly.

```csharp
// Card flip (Y-axis rotation)
"motion-[0:rotate-y-0,50:rotate-y-[180deg],100:rotate-y-[360deg]] " +
"motion-duration-3000ms motion-loop motion-ease-ease-in-out"

// Depth pop with translate-z
"motion-[0:translate-z-[-50px]_blur-[3px]_opacity-50_scale-[0.95]," +
"50:translate-z-[10px]_blur-0_opacity-100_scale-[1.02]," +
"100:translate-z-0_blur-0_opacity-100_scale-100] " +
"motion-duration-600ms motion-stagger-40ms motion-per-letter-loop motion-ease-ease-out"

// Cube face rotation (combined X + Y)
"motion-[0:rotate-x-0_rotate-y-0," +
"25:rotate-x-[90deg]_rotate-y-0," +
"50:rotate-x-[90deg]_rotate-y-[90deg]," +
"75:rotate-x-0_rotate-y-[90deg]," +
"100:rotate-x-0_rotate-y-0] " +
"motion-duration-4000ms motion-loop motion-ease-ease-in-out"
```

### Filter Animations in Keyframes

Filter functions animate smoothly inside `motion-[...]` keyframes. Crosswind auto-registers `@property` rules for filter-related custom properties, enabling proper interpolation.

Supported filter utilities: `blur`, `brightness`, `contrast`, `hue-rotate`, `saturate`, `grayscale`, `sepia`, `invert`.

```csharp
// Hue rotation cycle — rainbow color shifting
"motion-[0:hue-rotate-0,100:hue-rotate-[360deg]] " +
"motion-duration-3000ms motion-loop motion-ease-linear"

// Brightness flash
"motion-[0:brightness-100,15:brightness-[2],30:brightness-100,100:brightness-100] " +
"motion-duration-2000ms motion-loop"

// Saturate pulse
"motion-[0:saturate-100,50:saturate-[2],100:saturate-100] " +
"motion-duration-1500ms motion-loop motion-ease-ease-in-out"

// Grayscale fade
"motion-[0:grayscale-0,50:grayscale-100,100:grayscale-0] " +
"motion-duration-4000ms motion-loop motion-ease-ease-in-out"

// Combined filters (blur + brightness + hue-rotate)
"motion-[0:blur-0_brightness-100_hue-rotate-0," +
"25:blur-[2px]_brightness-[1.2]_hue-rotate-[45deg]," +
"50:blur-[4px]_brightness-[1.5]_hue-rotate-[90deg]," +
"75:blur-[2px]_brightness-[1.2]_hue-rotate-[135deg]," +
"100:blur-0_brightness-100_hue-rotate-[180deg]] " +
"motion-duration-4000ms motion-loop motion-ease-ease-in-out"
```

### Text Shadow Animations in Keyframes

`text-shadow-[...]` can be used inside keyframe steps for chromatic aberration and glow effects. Since text-shadow values are arbitrary, they use `'*'` syntax in `@property` and interpolate as whole values.

```csharp
// Chromatic aberration glitch
"glitch:motion-[0:text-shadow-[0_0_0_transparent,0_0_0_transparent]," +
"20:text-shadow-[3px_0_0_rgba(255,0,0,0.8),-3px_0_0_rgba(0,255,255,0.8)]," +
"40:text-shadow-[-2px_1px_0_rgba(255,0,0,0.6),2px_-1px_0_rgba(0,255,255,0.6)]," +
"60:text-shadow-[2px_0_0_rgba(255,0,0,0.8),-2px_0_0_rgba(0,255,255,0.8)]," +
"80:text-shadow-[-1px_-1px_0_rgba(255,0,0,0.5),1px_1px_0_rgba(0,255,255,0.5)]," +
"100:text-shadow-[0_0_0_transparent,0_0_0_transparent]] " +
"glitch:motion-duration-150ms glitch:motion-loop glitch:motion-ease-[steps(1)]"

// Neon glow pulse
"glow:motion-[0:text-shadow-[0_0_0_rgba(0,0,0,0)]," +
"25:text-shadow-[0_0_0.5em_rgba(255,0,128,0.5)]," +
"50:text-shadow-[0_0_0.8em_rgba(255,100,150,0.4)]," +
"75:text-shadow-[0.1em_0_0_rgba(255,0,100,0.6),-0.1em_0_0_rgba(255,150,200,0.6)]," +
"100:text-shadow-[0_0_0_rgba(0,0,0,0)]] " +
"glow:motion-duration-3000ms glow:motion-loop"
```

### Common Animation Patterns

Entry animations:

```csharp
public static class Enter
{
    public const string FadeUp =
        "motion-[0:opacity-0_translate-y-[12px],100:opacity-100_translate-y-0] " +
        "motion-duration-300ms motion-ease-[cubic-bezier(0.25,1,0.35,1)] motion-fill-both";

    public const string ScaleIn =
        "motion-[0:opacity-0_scale-[0.95],100:opacity-100_scale-100] " +
        "motion-duration-300ms motion-ease-[cubic-bezier(0.25,1,0.35,1)] motion-fill-both";
}
```

Hover effects (CSS transitions are often better for hover states):

```csharp
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
```

Looping effects:

```csharp
public static class Loop
{
    public const string Pulse =
        "motion-[0:opacity-70,50:opacity-100,100:opacity-70] " +
        "motion-duration-2000ms motion-loop motion-ease-ease-in-out";

    public const string Breathe =
        "motion-[0:scale-100,50:scale-[1.02],100:scale-100] " +
        "motion-duration-3000ms motion-loop motion-ease-ease-in-out";
}
```

### Animatable Properties

The following properties animate smoothly in `motion-[...]` keyframe animations:

- **Opacity**: `opacity`
- **2D transforms**: `translate-x`, `translate-y`, `scale`, `scale-x`, `scale-y`, `rotate`, `skew-x`, `skew-y`
- **3D transforms**: `rotate-x`, `rotate-y`, `translate-z`
- **Filter functions**: `blur`, `brightness`, `contrast`, `grayscale`, `hue-rotate`, `invert`, `saturate`, `sepia`
- **Colors**: `text-*`, `bg-*`, `border-*` (color values)
- **Text shadow**: `text-shadow-[...]` (arbitrary values)
- **Border properties**: `border-{n}` (width), `border-{color}`, `rounded-*` (border-radius)
- **Ring and outline**: `ring-{n}`, `outline-offset-{n}`
- **Box shadow**: `shadow-[...]` (arbitrary values)

Crosswind auto-registers `@property` rules for filter functions, transform variables, and typed custom properties (colors, lengths, angles, numbers). This enables smooth CSS interpolation without manual setup.

### Advanced Motion Utilities

Crosswind supports CSS Animations Level 2 properties for scroll-driven animations, composition control, and playback management.

#### Scroll Timelines

Declare a scroll timeline on a scroll container, then bind an animation track to it:

```csharp
// On the scroll container
"scroll-timeline-[--hero_y]"

// On the animated element
"lead:motion-[0:opacity-0,100:opacity-100] lead:motion-timeline-[--hero]"
```

#### Animation Composition

Control how multiple animations combine on the same element:

```csharp
// Additive composition — transforms blend instead of replacing
"pulse:motion-[0:scale-100,100:scale-110] pulse:motion-composition-add"
```

Values: `replace` (default), `add`, `accumulate`.

#### Play State Control

Pause and resume animations programmatically:

```csharp
"lead:motion-play-state-paused"    // starts paused
"lead:motion-play-state-running"   // resumes
```

#### Animation Range

Clamp animation playback to a portion of a scroll timeline:

```csharp
"halo:motion-range-[entry_0%_exit_60%]"
"halo:motion-range-start-[entry_10%]"
"halo:motion-range-end-[exit_90%]"
```

#### Motion Priority

Control stagger ordering with a priority hint (0–999):

```csharp
"motion-priority-0"     // default
"motion-priority-100"   // higher priority staggers first
```

## Complete Example

A button component combining multiple style aspects:

```csharp
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

// Usage
view.Button(style: [Button.Primary], label: "Submit", onClick: async () => { });
```

## Common Pitfalls and Solutions

### Full-Screen Layouts with Padding

Both approaches work correctly because Crosswind includes `box-sizing: border-box` in its preflight (like Tailwind):

```csharp
// Option 1: Padding on Root (preferred for semantic clarity)
UI.Root(style: ["h-screen bg-slate-950 p-4"], content: view =>
{
    view.Column(style: ["w-full h-full"], content: col => { ... });
});

// Option 2: Padding on inner container (also works)
UI.Root(style: ["h-screen bg-slate-950"], content: view =>
{
    view.Column(style: ["w-full h-full p-4"], content: col => { ... });
});
```

With `border-box`, padding is included in the element's dimensions, so `h-full p-4` means "100% height with padding inside" rather than "100% + padding".

### Width and Sizing Context

Design width with padding, alignment, and flex proportions — never hardcoded pixel widths for layout containers.

**Percentage widths need context:** Classes like `w-1/4` or `w-1/3` resolve against the parent's computed width. If the parent is `position: absolute` with no explicit width, or has `width: auto` without flex constraints, the percentage resolves to zero. The element with the percentage class must be a direct child of a flex/grid container or a parent with an explicit width.

```csharp
// WRONG — percentage on child of auto-width absolute element
view.Box(["absolute"], content: view =>
{
    view.Column(["w-1/4"], ...); // Collapses to zero!
});

// RIGHT — percentage on child of flex container
view.Row(["flex-1 min-w-0"], content: view =>
{
    view.Column(["w-1/4 flex-shrink-0"], ...); // 25% of parent flex item
    view.Column(["flex-1 min-w-0"], ...);      // Remaining space
});
```

**Panel pattern:** Use `Panel.*` theme constants for side panels — they bundle proportional width + minimum + `flex-shrink-0`:

| Constant | Value | Use case |
|---|---|---|
| `Panel.Sidebar` | `w-1/4 min-w-48 flex-shrink-0` | Standard sidebar |
| `Panel.SidebarNarrow` | `w-1/5 min-w-40 flex-shrink-0` | Compact sidebar |
| `Panel.Side` | `w-1/3 min-w-48 flex-shrink-0` | Log/artifact/detail panels |
| `Panel.Wide` | `w-2/5 min-w-64 flex-shrink-0` | Wide side panel |
| `Panel.Fill` | `flex-1 min-w-0` | Fluid content area |

**When pixel widths are acceptable:** Floating overlays (popovers, tooltips, dropdowns), small decorative elements (status dots, avatars), and fixed-size interactive controls. Never use pixel widths for layout-level containers like sidebars or content areas.

### Icon Sizing

Icons automatically size and center their SVG content. Just set width and height:

```csharp
row.Icon(style: ["w-4 h-4"], name: "message-circle");  // 16x16
row.Icon(style: ["w-5 h-5"], name: "settings");        // 20x20
```

Note: The Icon component internally uses `display: inline-flex` to ensure width/height CSS properties work correctly (CSS width/height don't apply to inline elements by default).

### Canonical Icon + Text Pattern

This is the robust, clean pattern that works everywhere:

```csharp
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
```

**Key rules:**
1. **Button**: Always set `text-white` (or appropriate color) - icons inherit text color
2. **Row**: Use `flex items-center gap-3` for alignment and spacing
3. **Icon**: Just `w-4 h-4` for size - no other classes needed
4. **Text**: No style needed - inherits from parent

### Common Mistakes

```csharp
// WRONG: Missing text color = black/invisible icons
style: ["bg-blue-600 ..."]

// CORRECT: Always include text color
style: ["text-white bg-blue-600 ..."]
```

### Standard Sizes

Use theme constants or explicit sizes:

| Size | Theme Constant | Classes | Pixels |
|------|----------------|---------|--------|
| Extra small | `Icon.Size.Xs` | `w-4 h-4 shrink-0` | 16px |
| Small | `Icon.Size.Sm` | `w-5 h-5 shrink-0` | 20px |
| Medium (default) | `Icon.Size.Md` | `w-6 h-6 shrink-0` | 24px |
| Large | `Icon.Size.Lg` | `w-8 h-8 shrink-0` | 32px |

Common gaps: `gap-2` (tight), `gap-3` (normal), `gap-4` (spacious)

### Fullscreen Effects and Overflow

Animations that translate elements outside their container bounds (e.g. a sweep band animating `translate-y` from `-100px` to `900px`) will trigger unwanted scrollbars. Always add `overflow-hidden` to the container that holds such overlay elements:

```csharp
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
```

This applies to any fullscreen overlay effect: scan lines, sweep bands, CRT overlays, vignettes with scale animations, etc.

### Responsive Breakpoints

**All UIs must be built responsively using breakpoint prefixes.** Never use server-side viewport detection or ad-hoc mechanisms — always use CSS breakpoints (`sm:`, `md:`, `lg:`, `xl:`, `2xl:`) for responsive behavior. Mobile-first means unprefixed styles apply to all sizes, then larger breakpoints override:

```csharp
// Padding: 12px on mobile, 16px on sm+, 24px on md+
view.Column(style: ["p-3 sm:p-4 md:p-6"], content: col => { ... });

// Hidden on mobile, visible on sm+
row.Text(text: "Projects", style: ["hidden sm:block"]);

// Different layouts per breakpoint
view.Column(style: ["flex flex-col sm:flex-row"], content: col => { ... });

// Sidebar: overlay on mobile, inline on desktop
view.Box(style: ["absolute inset-y-0 left-0 z-50 md:static md:z-auto"], ...);

// Backdrop: visible on mobile only
view.Box(style: ["absolute inset-0 bg-black/50 z-40 md:hidden"], ...);
```

Never hardcode sizes on content elements. Use responsive grids (`grid-cols-[repeat(auto-fill,minmax(220px,1fr))]`) for card layouts and `truncate` for text overflow instead of relying on fixed container widths.

## Sophisticated UI Design Patterns

### Visual Hierarchy with Gradients

Use gradients for primary actions and solid colors for secondary actions:

```csharp
// Primary button - gradient with shadow
view.Button(
    style: ["px-4 py-2 text-sm bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 rounded-xl font-medium transition-all shadow-lg"],
    label: "Primary Action",
    onClick: async () => { });

// Secondary button - subtle with border
view.Button(
    style: ["px-4 py-2 text-sm bg-slate-700/50 hover:bg-slate-600/50 text-slate-300 hover:text-white rounded-xl font-medium transition-all border border-slate-600/50"],
    label: "Secondary",
    onClick: async () => { });
```

### Icon Containers with Gradient Backgrounds

Wrap icons in styled containers for visual weight:

```csharp
container.Column(style: ["w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500/20 to-purple-500/20 flex items-center justify-center flex-shrink-0 border border-slate-600/30"], content: iconWrap =>
{
    iconWrap.Icon(style: ["w-4 h-4 text-blue-400"], name: "wrench");
});
```

### Cards and Containers

Use semi-transparent backgrounds with subtle borders:

```csharp
// Card with hover state
list.Column(style: [
    "bg-slate-800/50 rounded-2xl p-4 cursor-pointer transition-all border",
    isSelected ? "border-blue-500/50 bg-slate-800/80" : "border-slate-700/50 hover:bg-slate-700/50 hover:border-slate-600/50"
], content: card => { ... });

// Nested content container
details.Row(style: ["flex items-start gap-3 bg-slate-900/50 p-3 rounded-xl border border-slate-700/30"], content: row => { ... });
```

### Badges and Pills

Use rounded-full for pill-shaped badges:

```csharp
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
```

### Color Consistency

Maintain a cohesive color palette throughout the app:

| Role | Classes |
|------|---------|
| Primary accent | `from-blue-600 to-purple-600` (gradient) |
| Backgrounds | `slate-800/30`, `slate-900/50` (semi-transparent) |
| Borders | `slate-700/50`, `slate-600/50` (subtle) |
| Primary text | `text-white` |
| Secondary text | `text-slate-300` |
| Muted text | `text-slate-400`, `text-slate-500` |
| Accent icons | `text-blue-400`, `text-purple-400` |
| Neutral icons | `text-slate-400` |

### Spacing Scale

Use consistent spacing throughout:

```csharp
// Container padding (responsive)
"p-3 sm:p-4 md:p-6"

// Gap between items
"gap-2" // tight (8px)
"gap-3" // comfortable (12px)
"gap-4" // spacious (16px)

// Vertical spacing in lists
"space-y-2" // tight
"space-y-3" // comfortable
"space-y-4" // spacious
```

### Transitions and Interactions

Add subtle transitions for polished interactions:

```csharp
"transition-all duration-200" // for color and transform changes
"hover:bg-slate-700/50"       // subtle hover background
"hover:text-white"            // brighten text on hover
"hover:border-slate-600/50"   // subtle border change
```

## Related Documentation

- [Crosswind Motion Spec](crosswind-motion-spec.md) — formal grammar and syntax specification for the motion language
- [Crosswind Tailwind Spec](crosswind-tailwind-spec.md) — complete reference of all supported Tailwind utility classes


---

﻿# Crosswind Motion Spec

A Tailwind-inspired, class-based DSL to describe visual motion timelines and audio behaviors using only class strings. This spec defines **tokens, forms, and grammar**. It intentionally avoids runtime/implementation details.

---

## 0) Conventions

* **Case:** all keywords and identifiers are lowercase ASCII.
* **Separators:** classes are whitespace-separated. Prefixes are colon-separated.
* **Whitespace inside brackets `[...]`:** spaces are allowed; leading/trailing spaces are ignored.
* **Escaping inside `[...]`:** to include `]` use `\]`. Backslash escapes any following char.
* **Underscore separator:** inside `motion-[...]` step utilities, `_` outside of brackets and parentheses is treated as whitespace. This allows `opacity-0_translate-y-[12px]` as equivalent to `opacity-0 translate-y-[12px]`.
* **Numbers:** decimal (`12`, `0.25`) with optional sign; no thousands separators.
* **Durations:** `<number>ms` or `<number>s` (e.g., `150ms`, `0.2s`).
* **Easings:** keywords (`linear`, `ease-in`, `ease-out`, `ease-in-out`) or `cubic-bezier(a,b,c,d)` with `a..d` numbers.
* **Identifiers:** `[a-z][a-z0-9_-]*`.
* **Negative utilities:** Tailwind-style minus prefix is allowed: `-translate-x-1`.
* **Transform variables:** transform utilities with track prefixes emit CSS custom properties of the form `--tw-xform-<hash>`.
  The `<hash>` is a deterministic value derived from the track label and transform payload, ensuring variables remain unique
  across components and tracks.

---

## 1) Prefix Chain

A **class token** may be preceded by zero or more prefixes in this order:

```
<variant-prefix>:* <track-prefix>:* <core-directive>
```

* **Variant prefixes (reserved keyword forms):**
  * Element pseudo-classes: `hover`, `focus`, `active`, `visited`, `disabled`, `enabled`, `required`, `optional`, `invalid`, `valid`, `autofill`, `placeholder-shown`, `read-only`, `read-write`, `target`, `empty`, `focus-visible`, `focus-within`, `checked`, `indeterminate`, `in-range`, `out-of-range`, `default`, `first`, `last`, `first-of-type`, `last-of-type`, `only`, `only-child`, `only-of-type`, `odd`, `even`, `open`.
  * Pseudo-elements: `before`, `after`, `first-letter`, `first-line`, `selection`, `marker`, `placeholder`, `file`, `backdrop`.
  * Group/peer scopes: `group-<state>` and `peer-<state>` where `<state>` is a pseudo-class, pseudo-element, or explicit selector in brackets `[selector]`.
  * Attribute/state forms: `aria-<name>[-<value>]`, `data-<name>[-<value>]`, `lang-[<tag>]`, `has-[<selector>]`.
  * Theme scoping: `theme-<name>` (if theme variants are enabled).
* **Track prefix:** any identifier **not** matching a reserved variant. Tracks may refer to:
  * Responsive/media contexts: `sm`, `md`, `lg`, `xl`, `2xl`, `print`, `portrait`, `landscape`, `motion-reduce`, `motion-safe`, `pointer-hover`, `pointer-none`, `any-pointer-hover`, `any-pointer-none`, `contrast-more`, `contrast-less`.
  * Container queries: `min-<breakpoint>` / `max-<breakpoint>` (Tailwind breakpoint tokens) and `supports-[<condition>]` (underscores → spaces; appends `: var(--tw)` if missing a colon).
  * Color/direction scopes: `dark`, `light`, `rtl`, `ltr`.
  * Custom parent selectors: anything else (e.g., `.prose`, `#panel`, `[data-mode=hero]`).
  Zero or more track prefixes are allowed; the innermost (closest to the directive) owns that directive.

Examples:

```
hover:motion-[...]                 // variant only
glitch:motion-[...]                // track only
hover:glitch:motion-[...]          // variant + track
group-hover:glitch:motion-loop     // variant + track + flag
```

---

## 2) Core Directives (tokens)

All directives are classes (whitespace-separated items). Many accept **dash parameters** or **bracket payloads**.

### 2.1 Motion Timeline Block

```
motion-[ <step>( , <step> )* ]
```

* **Step:** `<time> : <step-utilities>`
* **Time:** either **percent** (`0..100` as integer or decimal without `%`) **or** **duration** (`<number>ms|s`).
  **Rule:** all steps in one block MUST use the same time basis (percent **or** duration).
* **Step utilities:** one or more **utility tokens**, separated by spaces.
  A utility token is any Tailwind-like utility (e.g., `opacity-0`, `scale-105`, `-translate-x-1`) **or** a property with arbitrary value `name-[value]`.

Examples:

```
motion-[0:opacity-0,100:opacity-100]
motion-[0:opacity-0 scale-95, 100:opacity-100 scale-100]
glitch:motion-[0:opacity-100 -translate-x-1,10:opacity-0 translate-x-1,20:opacity-100 translate-x-0]
```

### 2.2 Motion Track Timing & Flags

All may be prefixed by variants and/or a track.

* `motion-duration-<dur>` or `motion-duration-[<dur>]`
  `<dur>` is a duration literal (`250ms`, `0.6s`).
* `motion-rate-<pct>` or `motion-rate-[<number>]`
  Playback rate multiplier (e.g., `150` = 150%). **Syntax only**; semantics are impl-defined.
* `motion-ease-<keyword>` or `motion-ease-[cubic-bezier(...)]`
* `motion-delay-<dur>` or `motion-delay-[<dur>]` (alias: `motion-track-delay-<dur>`)
  Delay applied to the entire track before playback starts.
* `motion-timeline-<keyword>` or `motion-timeline-[<timeline>]`
  Assigns the CSS `animation-timeline` value for the track. Bracket payloads support arbitrary timeline expressions (`scroll()`, `view()`, custom timeline names, etc.).
* `motion-composition-<mode>` or `motion-composition-[<mode>]`
  Maps to `animation-composition`; `<mode>` follows CSS Animations Level 2 (`replace|add|accumulate`).
* `motion-play-state-<state>` or `motion-play-state-[<state>]`
  Controls `animation-play-state` (`running|paused` plus arbitrary states for future expansion).
* `motion-range-[<value>]`
  Emits `animation-range` for the track. Payloads follow CSS shorthand syntax (e.g., `entry 10% exit 90%`).
* `motion-range-start-[<value>]` / `motion-range-end-[<value>]`
  Emit `animation-range-start` and `animation-range-end` respectively. Values support keywords (`normal|entry|exit|contain|cover`) and arbitrary timeline offsets.
* `motion-letter-delay-<dur>` or `motion-letter-delay-[<dur>]` (sequential offset; alias: `motion-stagger-<dur>`)
* `motion-fill-<mode>` where `<mode>` ∈ `none|forwards|backwards|both`
* `motion-loop` (boolean flag)
* `motion-once` (boolean flag)
* `motion-ping-pong` (boolean flag; alternates playback direction)
* `motion-per-letter` (boolean flag)
* `motion-per-letter-loop` (boolean flag; combines `motion-per-letter` with `motion-loop`)
* `motion-per-letter-ping-pong` (boolean flag; combines `motion-per-letter` with `motion-ping-pong`)
* `motion-per-letter-reverse` (boolean flag; pairs with `motion-per-letter` to stagger from the last glyph backward)
* `motion-per-letter-reverse-loop` (boolean flag; combines `motion-per-letter-reverse` with `motion-loop`)
* `motion-per-word` (boolean flag)
* `motion-per-word-loop` (boolean flag; combines `motion-per-word` with `motion-loop`)
* `motion-per-word-ping-pong` (boolean flag; combines `motion-per-word` with `motion-ping-pong`)
* `motion-per-word-reverse` (boolean flag; pairs with `motion-per-word` to stagger from the last word backward)
* `motion-per-word-reverse-loop` (boolean flag; combines `motion-per-word-reverse` with `motion-loop`)
* `motion-per-line` (boolean flag)
* `motion-per-line-loop` (boolean flag; combines `motion-per-line` with `motion-loop`)
* `motion-per-line-ping-pong` (boolean flag; combines `motion-per-line` with `motion-ping-pong`)
* `motion-per-line-reverse` (boolean flag; pairs with `motion-per-line` to stagger from the last line backward)
* `motion-per-line-reverse-loop` (boolean flag; combines `motion-per-line-reverse` with `motion-loop`)
* `motion-per-paragraph` (boolean flag)
* `motion-per-children` (boolean flag)
* `motion-priority-<int>` or `motion-priority-[<int>]` (track precedence hint; syntax only)

Examples:

```
glow:motion-duration-800ms glow:motion-ease-ease-in-out glow:motion-loop
motion-delay-[120ms] motion-fill-both
title:motion-per-letter title:motion-letter-delay-60ms
```

**Per-track semantics.** Each Level 2 utility stores its value on the addressed track. During compilation Crosswind emits
comma-separated longhands only when a track overrides that property and fills in defaults (`auto`, `replace`, `running`,
`normal`) for tracks that omit them, so you can pause or retarget one layer without affecting the others.

Concrete patterns:

```html
<div class="scroll-timeline-[--hero y]">
  <div class="lead:motion-[0:opacity-0,100:opacity-100] lead:motion-timeline-[--hero]
              lead:motion-play-state-paused body:motion-[0:scale-95,100:scale-100]">
    ...
  </div>
</div>
```

* `lead:` binds its track to the scroll timeline and starts paused so a class toggle can resume it while the unprefixed track
  plays immediately.
* Adding `flare:motion-composition-add` later yields `animation-composition: replace, add, replace`, letting the additive track
  blend with the existing ones instead of replacing them.

```html
<button class="pulse:motion-[0:scale-100,100:scale-110] pulse:motion-composition-add
                 halo:motion-[0:opacity-0,100:opacity-100] halo:motion-range-[entry 0% exit 60%]">
  CTA
</button>
```

* `pulse:` accumulates with other transforms while `halo:` clamps its playback window to the first 60 % of the timeline.

### 2.3 Input Binding

Binds a **virtual timeline progress** (0–100) to an input source.

* **Binding source & range**

  ```
  motion-bind-<source>/[<min>-<max>]
  ```

  `<source>` ∈ `scrollx|scrolly|dragx|dragy|value|time`.
  `<min>`, `<max>` are numbers (units depend on source; syntax only).

* **Clamp policy** (empty brackets default to `clamp`)

  ```
  motion-bind-[clamp]           // default
  motion-bind-[freeze-start]
  motion-bind-[freeze-end]
  motion-bind-[unclamped]
  ```

* **Direction**

  ```
  motion-bind-reverse
  ```

* **Input easing**

  ```
  motion-bind-ease-<keyword>
  motion-bind-ease-[cubic-bezier(...)]
  ```

* **Output mapping (window remap)**

  ```
  motion-map-[<a>..<b>-><c>..<d>]
  ```

  All `<a..d>` are 0–100 numbers (syntax).

> **Relation to CSS scroll-driven animation primitives.**
> Crosswind now emits CSS Animations Level 2 longhands (`animation-timeline`, `animation-range`,
> `animation-composition`, `animation-play-state`) when the matching utilities are present.
> Use `scroll-timeline-[...]` on scrolling containers to declare timelines and pair them with
> `motion-timeline-*` track assignments. Existing `motion-bind-*` directives remain available for
> declarative metadata and backwards compatibility with runtime-driven bindings.

Examples:

```
motion-bind-scrolly/[100-500] motion-bind-ease-[cubic-bezier(0.4,0,0.2,1)] motion-bind-[clamp]
motion-map-[0..100->10..90] motion-bind-reverse
```

### 2.4 Scroll Timelines

```
scroll-timeline-[<value>]
```

* Emits the `scroll-timeline` shorthand on the current element.
* Payloads can combine timeline names and axes (e.g., `scroll-timeline-[--hero x]`) or function-style definitions (`scroll()`, `view()`).
* Multiple values are supported via comma-separated payloads inside the brackets.

### 2.5 Audio: Sources, Triggers, Automation

> **Note:** The audio directives described in this section define syntax for future implementation. They are not currently processed by the Crosswind compiler.

#### 2.5.1 Source Attachment

```
sfx:source-[<uri-or-id>]
```

* Payload accepts any non-empty string; `]` must be escaped as `\]`.
* Multiple sources may be specified with different track prefixes or IDs (see below).

Optional hints (boolean / identifiers):

```
sfx:prime                      // hint to prime/preload a source
sfx:id-[<identifier>]          // label a source for reuse (syntax)
```

#### 2.5.2 Trigger Playback

```
<variant>:sfx:play
<variant>:sfx:play-[<id>]      // target a labeled source
```

* Without `[<id>]`, targets the nearest `sfx:source-[...]` in the same class list (syntax ordering is not enforced; selection is impl-defined).

#### 2.5.3 Parameter Automation (within motion steps)

Audio parameters are expressible as utilities inside `motion-[...]` steps:

```
volume-[<0..1>]
pitch-[<number>]               // 1.0 = normal
pan-[-1..1]
reverb-[<0..1>]
filter-[<number><unit>?]       // e.g., 800Hz, 1kHz (unit text is free-form)
```

Examples:

```
sfx:source-[/snd/zap.wav] sfx:prime hover:sfx:play
sfx:motion-[0:volume-[0],10:volume-[1] pan-[-0.3],100:volume-[0]]
click:sfx:play-[zap]
sfx:id-[zap] sfx:source-[/snd/zap.wav]
```

#### 2.5.4 Shorthand Alias (optional)

```
<variant>:sound-[<uri>]
```

**Purely syntactic sugar** equivalent to:

```
sfx:source-[<uri>] <variant>:sfx:play
```

### 2.6 Presets & Track Aliases

* **Preset expansion**

  ```
  motion-track-<name>
  ```

  A macro token that expands (outside this spec’s scope) into one or more `motion-*` and/or `motion-[...]` directives.

* **Named track timelines (inline)**

  ```
  <track>:motion-[...]
  <track>:motion-duration-...
  ```

  `<track>` is any identifier.

Examples:

```
motion-track-glow glitch:motion-[...]
glitch:motion-duration-300ms glitch:motion-loop
```

---

## 3) Utility Tokens inside `motion-[...]`

Inside a timeline step, **utilities** follow Tailwind forms:

* **Named tokens:** `opacity-0`, `scale-105`, `shadow-xl`, etc.
* **Negative prefix:** `-translate-x-1`, `-rotate-3`.
* **Arbitrary values:** `translate-x-[3.5rem]`, `rotate-[12deg]`, `shadow-[0_0_10px_rgba(0,0,0,0.4)]`.
* **Audio params:** as listed in §2.5.3.

**Separator:** single space or `_` (underscore) between utilities. Underscores outside of brackets and parentheses are normalized to spaces.
**Commas:** separate steps.
**Colons:** separate `time` and `utilities` in a step.

Example (mixed utilities):

```
motion-[0:opacity-0 -translate-y-[8px], 100:opacity-100 translate-y-0]
```

---

## 4) Variant Prefixes (reserved list)

The following identifiers are **reserved** as variants when used as a prefix ending with `:`:

* Element pseudo-classes and pseudo-elements listed in §1.
* `group-<state>`, `peer-<state>`, `aria-<name>[-<value>]`, `data-<name>[-<value>]`, `lang-[<tag>]`, `has-[<selector>]`, `theme-<name>`.

> Any other prefix is treated as a **track** label (see §1).

---

## 5) Formal Grammar (EBNF)

> Notes:
> • Terminals in **bold**; literals in quotes.
> • `_` denotes optional whitespace.
> • This grammar covers syntax only; semantic constraints are listed after.

```
document        ::= ( WS? class-token WS? )*

class-token     ::= (prefix ":")* core-directive

prefix          ::= variant-prefix | track-prefix

variant-prefix  ::= pseudo-class-variant | pseudo-element-variant
                  | group-variant | peer-variant
                  | aria-variant | data-variant
                  | lang-variant | has-variant
                  | theme-variant

track-prefix    ::= identifier | supports-track | selector-track

core-directive  ::= motion-block
                  | motion-param
                  | motion-flag
                  | bind-directive
                  | map-directive
                  | scroll-timeline-directive
                  | audio-source
                  | audio-trigger
                  | audio-flag
                  | preset

scroll-timeline-directive ::= "scroll-timeline-" bracket-value

motion-block    ::= "motion-" bracket-steps

bracket-steps   ::= "[" WS? step ( WS? "," WS? step )* WS? "]"

step            ::= time WS? ":" WS? step-utilities

time            ::= percent | duration

percent         ::= number        // 0..100 recommended; '%' not written
duration        ::= number ( "ms" | "s" )

step-utilities  ::= utility ( ( WS+ | "_" ) utility )*

utility         ::= neg? utility-name ( "-" utility-atom )*
                  | utility-name "-" bracket-value
                  | audio-param

neg             ::= "-"

utility-name    ::= identifier

utility-atom    ::= identifier | number

audio-param     ::= ( "volume" | "pitch" | "pan" | "reverb" | "filter" ) "-" bracket-value

motion-param    ::= "motion-duration-"      duration-or-bracket
                  | "motion-rate-"          number-or-bracket
                  | "motion-ease-"          easing-or-bracket
                  | "motion-delay-"         duration-or-bracket
                  | "motion-track-delay-"   duration-or-bracket
                  | "motion-letter-delay-"  duration-or-bracket
                  | "motion-stagger-"       duration-or-bracket
                  | "motion-fill-"          fill-mode
                  | "motion-priority-"      int-or-bracket
                  | "motion-timeline-"      identifier-or-bracket
                  | "motion-composition-"   identifier-or-bracket
                  | "motion-play-state-"    identifier-or-bracket
                  | "motion-range-"         bracket-value
                  | "motion-range-start-"   bracket-value
                  | "motion-range-end-"     bracket-value

duration-or-bracket   ::= duration | bracket-value
number-or-bracket     ::= number   | bracket-value
easing-or-bracket     ::= easing   | bracket-value
int-or-bracket        ::= integer  | bracket-value
identifier-or-bracket ::= identifier | bracket-value

fill-mode      ::= "none" | "forwards" | "backwards" | "both"

motion-flag    ::= "motion-loop" | "motion-once" | "motion-ping-pong"
                 | per-element-flag
                 | "motion-per-paragraph"
                 | "motion-per-children"

per-element-flag ::= ( "motion-per-letter" | "motion-per-word" | "motion-per-line" )
                     ( "-loop" | "-ping-pong" | "-reverse" | "-reverse-loop" )?

bind-directive ::= "motion-bind-" bind-source "/" bracket-range
                 | "motion-bind-ease-" easing-or-bracket
                 | "motion-bind-" bracket-clamp
                 | "motion-bind-reverse"

bind-source    ::= "scrollx" | "scrolly" | "dragx" | "dragy" | "value" | "time"

map-directive  ::= "motion-map-" bracket-map

audio-source   ::= "sfx:source-" bracket-value
                 | "sfx:id-"     bracket-id

audio-flag     ::= "sfx:prime"

audio-trigger  ::= "sfx:play" ( "-" bracket-id )?
                 | "sound-" bracket-value     // shorthand

preset         ::= "motion-track-" identifier

// --- Bracketed payloads ---

bracket-value  ::= "[" bracket-chars "]"
bracket-id     ::= "[" identifier "]"

bracket-range  ::= "[" WS? number WS? "-" WS? number WS? "]"
bracket-clamp  ::= "[" ( "clamp" | "freeze-start" | "freeze-end" | "unclamped" )? "]"
bracket-map    ::= "[" WS? number WS? ".." WS? number WS? "->"
                        WS? number WS? ".." WS? number WS? "]"

// --- Lexical ---

identifier     ::= lc-alpha ( lc-alpha | digit | "_" | "-" )*
number         ::= sign? ( digit+ ( "." digit* )? | "." digit+ )
integer        ::= sign? digit+
sign           ::= "+" | "-"
digit          ::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
lc-alpha       ::= "a" | "b" | ... | "z"

bracket-chars  ::= ( escaped | char-no-bracket )*
escaped        ::= "\" any-char
char-no-bracket::= any-char-except-] | "]" if escaped
any-char       ::= any Unicode scalar value
WS             ::= space | tab | newline
WS+            ::= WS WS*
supports-track ::= "supports-" bracket-value

selector-track ::= "." identifier | "#" identifier | bracket-value

pseudo-class-variant ::= "hover" | "focus" | "active" | "visited" | "disabled" | "enabled"
                       | "required" | "optional" | "invalid" | "valid" | "autofill"
                       | "placeholder-shown" | "read-only" | "read-write" | "target"
                       | "empty" | "focus-visible" | "focus-within" | "checked"
                       | "indeterminate" | "in-range" | "out-of-range" | "default"
                       | "first" | "last" | "first-of-type" | "last-of-type"
                       | "only" | "only-child" | "only-of-type" | "odd" | "even" | "open"

pseudo-element-variant ::= "before" | "after" | "first-letter" | "first-line"
                         | "selection" | "marker" | "placeholder" | "file" | "backdrop"

group-variant ::= "group-" ( identifier | bracket-value | pseudo-class-variant | pseudo-element-variant )

peer-variant  ::= "peer-" ( identifier | bracket-value | pseudo-class-variant | pseudo-element-variant )

aria-variant  ::= "aria-" identifier ( "-" identifier )?

data-variant  ::= "data-" identifier ( "-" identifier )?

lang-variant  ::= "lang-" bracket-value

has-variant   ::= "has-" bracket-value

theme-variant ::= "theme-" identifier
```

---

## 6) Semantic-Light Validation Rules (still **syntax-level** constraints)

These are minimal rules code generators MAY enforce during parse; they do not prescribe behavior.

1. **Timeline basis consistency:** every `motion-[...]` block MUST use either all percent times **or** all durations.
2. **Step form:** each step MUST contain exactly one time, a colon, and ≥1 utility token.
3. **Easing function text** inside `[...]` MUST be fully contained (balanced parentheses) and may include commas and spaces.
4. **Bracket payloads** MUST close; `\]` is the only way to include a literal `]`.
5. **Variant collisions:** multiple variants are allowed; order is preserved as written (left-to-right).
6. **Track labeling:** the **nearest** track prefix to a directive labels that directive. Outer track prefixes are inert for that directive.
7. **Alias `sound-[...]`:** purely syntactic; parsers MAY normalize it to `sfx:source-[...] <same-prefixes>:sfx:play`.

---

## 7) Canonicalization (optional, for generators)

Parsers MAY normalize input to a canonical form:

* Trim extra spaces around commas/colons inside `motion-[...]`.
* Convert `translate-x--1` to `-translate-x-1`.
* Collapse duplicate flags on the same directive (`motion-loop motion-loop` → single).
* Normalize duration literals (`.2s` → `0.2s`).
* Preserve user-written order of **steps** and **prefixes**.

---

## 8) Examples (normative data points)

**A. Basic fade**

```
motion-[0:opacity-0,100:opacity-100]
```

**B. Track + timing + loop**

```
glow:motion-[0:shadow-sm,50:shadow-xl,100:shadow-sm]
glow:motion-duration-800ms glow:motion-ease-ease-in-out glow:motion-loop
```

**C. Interaction + track + timeline**

```
hover:glitch:motion-[0:opacity-100 -translate-x-1,10:opacity-0 translate-x-1,20:opacity-100 translate-x-0]
```

**D. Per-letter + stagger**

```
title:motion-per-letter title:motion-letter-delay-60ms
title:motion-[0:opacity-0 translate-y-[8px],100:opacity-100 translate-y-0]
```

**E. Input binding + map**

```
motion-bind-scrolly/[100-500] motion-bind-ease-[cubic-bezier(0.4,0,0.2,1)] motion-bind-[clamp]
motion-map-[0..100->10..90]
```

**F. Audio with trigger + automation**

```
sfx:id-[zap] sfx:source-[/snd/zap.wav] sfx:prime
hover:sfx:play-[zap]
sfx:motion-[0:volume-[0],10:volume-[1] pan-[-0.3],100:volume-[0]]
```

**G. Shorthand sound alias**

```
active:sound-[tap.mp3]
```

**H. State-driven exit animation**

```
data-state-closed:motion-[0:opacity-100,100:opacity-0] data-state-closed:motion-duration-200ms
```

**I. Preset + inline override**

```
motion-track-glow glow:motion-duration-1200ms
```

**J. 3D transform (Y-axis card flip)**

```
motion-[0:rotate-y-0,50:rotate-y-[180deg],100:rotate-y-[360deg]]
motion-duration-3000ms motion-loop motion-ease-ease-in-out
```

**K. Filter animation (hue rotation cycle)**

```
motion-[0:hue-rotate-0,100:hue-rotate-[360deg]]
motion-duration-3000ms motion-loop motion-ease-linear
```

**L. Per-word compound variant (reverse loop)**

```
motion-[0:translate-x-0,50:translate-x-[5px],100:translate-x-0]
motion-duration-400ms motion-stagger-120ms motion-per-word-reverse-loop motion-ease-ease-in-out
```

**M. Scroll-driven animation with range**

```
scroll-timeline-[--hero y]
lead:motion-[0:opacity-0,100:opacity-100] lead:motion-timeline-[--hero] lead:motion-range-[entry 10% exit 90%]
```

---

## 9) Reserved Words

`motion`, `sfx`, `sound`, `motion-track`, `motion-duration`, `motion-rate`, `motion-ease`, `motion-delay`, `motion-track-delay`, `motion-letter-delay`, `motion-stagger`, `motion-fill`, `motion-loop`, `motion-once`, `motion-ping-pong`, `motion-per-letter`, `motion-per-letter-loop`, `motion-per-letter-ping-pong`, `motion-per-letter-reverse`, `motion-per-letter-reverse-loop`, `motion-per-word`, `motion-per-word-loop`, `motion-per-word-ping-pong`, `motion-per-word-reverse`, `motion-per-word-reverse-loop`, `motion-per-line`, `motion-per-line-loop`, `motion-per-line-ping-pong`, `motion-per-line-reverse`, `motion-per-line-reverse-loop`, `motion-per-paragraph`, `motion-per-children`, `motion-priority`, `motion-timeline`, `motion-composition`, `motion-play-state`, `motion-range`, `motion-range-start`, `motion-range-end`, `scroll-timeline`, `motion-bind`, `motion-bind-ease`, `motion-bind-reverse`, `motion-map`, `volume`, `pitch`, `pan`, `reverb`, `filter` and all **variant** names in §4.

---

## 10) Forward-Compatibility Notes

* New **variants** and **bind sources** may be added; unrecognized prefixes not in §4 are treated as **track** labels by this grammar.
* New **audio params** MAY appear as `name-[value]` utilities inside `motion-[...]` without changing the grammar.

---

### Parsing Checklist

1. Split by whitespace → `class-token`s.
2. For each token: parse `prefix ":"` chain (variants/tracks) → `core-directive`.
3. If `motion-[...]`: parse steps; enforce single timing basis; split utilities by spaces.
4. If timing/flags: record parameter or boolean.
5. If binding/map/ease: record payloads.
6. If audio: record sources, optional id, triggers, and step-level params.
7. If shorthand `sound-[...]`: expand to `sfx:source` + `<same-prefixes>:sfx:play`.
8. Preserve written order (prefixes and steps).


---

﻿# Crosswind Tailwind Spec

Below is a clean, exhaustive list of Tailwind CSS v4.x utility families, organized according to the official documentation structure. For each family, the canonical class pattern(s) are shown, including axis/directional shorthands, plus brief notes where v4 renamed or expanded functionality.

* The categories and families are based on Tailwind's v4 "Styling with utility classes" index, which enumerates every utility family by CSS property.
* Items documented under other pages (e.g. rings under box-shadow, gradient stops under background-image) have been incorporated.
* Where v4 removed or renamed families relative to v3, notes reference the v4 upgrade guide.

> **Notation & patterns**
>
> * `{value}` denotes Tailwind's normal value scale plus arbitrary values via brackets: e.g. `w-[3.75rem]`, `bg-[paint(worklet)]`.
> * `{n}` represents any (non‑negative) integer; v4 supports "any number" for many numeric families like `grid-cols-{n}`.
> * Axes/edges: `x|y|t|r|b|l`, logical block/inline variants where applicable (e.g. scroll snap).
> * Colors accept the normal palette or arbitrary values; transparency uses the `/alpha` suffix (e.g. `bg-red-500/20`). Families like `bg-opacity-*`, `text-opacity-*`, etc. were removed in v4.

---

## 1) Layout

* **Container**: `container` (responsive width constraints + centered)
* **Breaks**: `break-after-{auto|avoid|all|...}`, `break-before-{...}`, `break-inside-{auto|avoid|avoid-page|avoid-column}`
* **Box decoration**: `box-decoration-{clone|slice}`
* **Box sizing**: `box-{border|content}`
* **Display**: `block`, `inline`, `inline-block`, `contents`, `flow-root`, `table`, `inline-table`, `table-caption`, `table-cell`, `table-column`, `table-column-group`, `table-footer-group`, `table-header-group`, `table-row-group`, `table-row`, `list-item`, `hidden`, `flex`, `inline-flex`, `grid`, `inline-grid`
* **Float / Clear**: `float-{left|right|none}`, `clear-{left|right|both|none}`
* **Isolation**: `isolate`, `isolation-auto`
* **Object fit**: `object-{contain|cover|fill|none|scale-down}`
* **Object position**: `object-{bottom|center|left|left-bottom|left-top|right|right-bottom|right-top|top}` or `object-[<position>]`
* **Overflow**: `overflow-{auto|hidden|clip|visible|scroll}`, `overflow-x-*`, `overflow-y-*`
* **Overscroll behavior**: `overscroll-{auto|contain|none}`, `overscroll-x-*`, `overscroll-y-*`
* **Position**: `static`, `fixed`, `absolute`, `relative`, `sticky`
* **Offsets** (inset/edges): `inset-{value}`, `inset-x-{value}`, `inset-y-{value}`, `top-{value}`, `right-{value}`, `bottom-{value}`, `left-{value}`, `start-{value}`, `end-{value}` (supports tokens & arbitrary)
* **Z-index**: `z-{value}` (supports auto, negative values)
* **Visibility**: `visible`, `invisible`, `collapse`
* **Columns**: `columns-{value}`
* **Aspect ratio**: `aspect-{auto|square|video|[ratio]}`

## 2) Flexbox & Grid

* **Flex basis**: `basis-{value}`
* **Flex direction**: `flex-{row|row-reverse|col|col-reverse}`
* **Flex wrap**: `flex-{wrap|wrap-reverse|nowrap}`
* **Order**: `order-{first|last|none|{n}}`
* **Flex shorthand**: `flex-1`, `flex-auto`, `flex-initial`, `flex-none`
* **Grow/Shrink**: `grow`, `grow-0`, `shrink`, `shrink-0`
* **Align content/items/self**:

  * `content-{normal|center|start|end|between|around|evenly|baseline|stretch}`
  * `items-{start|end|center|baseline|stretch}`
  * `self-{auto|start|end|center|stretch|baseline}`
* **Place content/items/self**: `place-content-{...}`, `place-items-{...}`, `place-self-{...}`
* **Justify content/items/self**: `justify-{start|end|center|between|around|evenly}`, `justify-items-{start|end|center|stretch}`, `justify-self-{auto|start|end|center|stretch}`
* **Gap**: `gap-{value}`, `gap-x-{value}`, `gap-y-{value}`
* **Grid templates**: `grid-cols-{n}`, `grid-rows-{n}`
* **Grid lines**: `col-{auto|span-{n}|start-{n}|end-{n}}`, `row-{auto|span-{n}|start-{n}|end-{n}}`
* **Grid auto**: `grid-flow-{row|col|dense|row-dense|col-dense}`, `auto-cols-{min|max|fr|auto|[...]}`, `auto-rows-{min|max|fr|auto|[...]}`

## 3) Spacing

* **Padding**: `p-{value}`, `px-*`, `py-*`, `pt|pr|pb|pl-*`, `ps|pe-*` (logical inline-start/end)
* **Margin**: `m-{value}`, `mx-*`, `my-*`, `mt|mr|mb|ml-*`, `ms|me-*` (logical inline-start/end; supports negative values, e.g. `-mt-2`, `-m-[3px]`)
* **Space between** (siblings): `space-x-{value}`, `space-y-{value}` (+ `space-x-reverse`, `space-y-reverse`).

## 4) Sizing

* **Width**: `w-{value}` (includes `w-{fraction}`, `w-{screen|min|max|fit}`, arbitrary)
* **Min/Max width**: `min-w-{value}`, `max-w-{value}`
* **Height**: `h-{value}` (includes `h-{screen|min|max|fit}`, arbitrary)
* **Min/Max height**: `min-h-{value}`, `max-h-{value}`
* **Square size (both axes)**: `size-{value}`

## 5) Typography

* **Font family**: `font-{sans|serif|mono}` or `font-[...]`
* **Font size**: `text-{xs|sm|base|lg|xl|2xl|...|9xl}` or `text-[...]`
* **Font smoothing**: `antialiased`, `subpixel-antialiased`
* **Font style/stretch/variant**: `italic`, `not-italic`; `font-stretch-{ultra-condensed...ultra-expanded}`; `normal-nums`, `slashed-zero`, `lining-nums`, etc.
* **Font palette (COLRv1)**: `font-palette-{normal|light|dark|match-parent}` or `font-palette-[--custom]`; define palettes with `font-palette-values-[--id font=<family> base=<n> 0=<color> ...]`.
* **Font weight**: `font-{thin|extralight|light|normal|medium|semibold|bold|extrabold|black}`
* **Leading (line-height)**: `leading-{none|tight|snug|normal|relaxed|loose|[number]|[length]}`
* **Lists**: `list-{inside|outside}`, `list-{disc|decimal|...}`, `list-image-[url(...)]`
* **Text align**: `text-{left|center|right|justify|start|end}`
* **Text decoration**:

  * Line: `underline`, `overline`, `line-through`, `no-underline` (`decoration-none` also acceptable)
  * Color: `decoration-{color}`
  * Style: `decoration-{solid|double|dotted|dashed|wavy}`
  * Thickness: `decoration-{from-font|[length]}`
  * Underline offset: `underline-offset-{auto|[length]}`
* **Text indent/overflow/transform/wrap**: `indent-{value}`, `text-{ellipsis|clip}`, `truncate`, `uppercase|lowercase|capitalize|normal-case`, `text-wrap-{wrap|nowrap|balance|pretty}`
* **Line clamp**: `line-clamp-{n}` (multi-line text truncation)
* **Content**: `content-[value]` (sets CSS `content` property, for `before`/`after` pseudo-elements)
* **Vertical align**: `align-{baseline|top|middle|bottom|text-top|text-bottom|sub|super}`
* **Whitespace / word-break / hyphens**: `whitespace-{normal|nowrap|pre|pre-line|pre-wrap|break-spaces}`, `break-{normal|words|all|keep}`, `hyphens-{none|manual|auto}`
* **Text color**: `text-{color}`

## 6) Backgrounds

* **Attachment/clip/origin/position/repeat/size**:
  `bg-{fixed|local|scroll}`, `bg-clip-{border|padding|content|text}`, `bg-origin-{border|padding|content}`,
  `bg-{bottom|center|left|left-bottom|left-top|right|right-bottom|right-top|top}` or `bg-[position]`,
  `bg-{repeat|no-repeat|repeat-x|repeat-y|repeat-round|repeat-space}`,
  `bg-{auto|cover|contain|[length]|[percentage]}`
* **Images (including gradients)**:

  * **Directional gradients**: `bg-linear-to-{t|tr|r|br|b|bl|l|tl}` (aliases `bg-gradient-to-*` remain documented/used)
  * **Angles**: `bg-linear-{angle}` (e.g. `bg-linear-45`)
  * **Color stops**: `from-{color}`, `via-{color}`, `to-{color}` (+ `via-none`).
  * **Custom images**: `bg-[url('...')]`, etc.
* **Background color**: `bg-{color}`

## 7) Borders (incl. Outline)

* **Border width**: `border-{value}`, `border-x|y|t|r|b|l|s|e-{value}` (+ `divide-x|y-{value}` for sibling dividers; supports reverse modifiers).
* **Border color/style**: `border-{color}`, `border-{solid|dashed|dotted|double|hidden|none}`
* **Border radius**: `rounded`, `rounded-{none|sm|md|lg|xl|2xl|3xl|full}`, edge/corner variants `rounded-{t|r|b|l|s|e}-{...}`, `rounded-{tl|tr|br|bl|ss|se|es|ee}-{...}`
* **Outline**: `outline-*` family (`outline-{[length]|[style]|none|hidden}`, `outline-{color}`, `outline-offset-{length}`) — v4 has dedicated width/color/style/offset utilities.

## 8) Effects (shadows, rings, blend, opacity)

* **Box shadow**: `shadow`, `shadow-{2xs|xs|sm|md|lg|xl|2xl}`, `shadow-none`, `shadow-inner`, `shadow-{color}`
* **Rings** (implemented via shadow):
  `ring-{n}` (width), `ring-{color}`, `ring-inset`, plus inset rings `inset-ring-{n}`, `inset-ring-{color}`, and ring offset: `ring-offset-{n}`, `ring-offset-{color}`.
* **Mix / background blend**: `mix-blend-{mode}`, `bg-blend-{mode}`
* **Opacity** (element): `opacity-{0..100}`
* **Text shadow**: `text-shadow`, `text-shadow-{xs|sm|md|lg|xl|2xl}`, `text-shadow-none` (added in v4.1).

## 9) Filters & Backdrop filters

* **Filter primitives** (auto-emitting `filter`):
  `blur-{xs|sm|md|lg|xl|2xl|3xl|[length]}`, `brightness-{value}`, `contrast-{value}`, `drop-shadow`, `drop-shadow-{sm|md|lg|xl|2xl}`, `grayscale`, `hue-rotate-{deg}`, `invert`, `saturate-{value}`, `sepia`
* **Optional filter toggle**: `filter`, `filter-none`
* **Backdrop primitives**:
  `backdrop-blur-{...}`, `backdrop-brightness-{...}`, `backdrop-contrast-{...}`, `backdrop-grayscale`, `backdrop-hue-rotate-{deg}`, `backdrop-invert`, `backdrop-opacity-{...}`, `backdrop-saturate-{...}`, `backdrop-sepia`
* **Backdrop filter toggle**: `backdrop-filter`, `backdrop-filter-none`

## 10) Tables

* **Border collapse**: `border-{collapse|separate}`
* **Border spacing**: `border-spacing-{value}`, `border-spacing-x-{value}`, `border-spacing-y-{value}`
* **Table layout**: `table-{auto|fixed}`
* **Caption side**: `caption-{top|bottom}`

## 11) Transitions & Animation

* **Transition**: `transition` (all), `transition-none`, `transition-{property}`
* **Timing**: `duration-{ms}`, `ease-{linear|in|out|in-out|[cubic-bezier]}`, `delay-{ms}`
* **Behavior**: `transition-behavior-{normal|allow-discrete}` (added in v4.1)
* **Animation**: `animate-{spin|ping|pulse|bounce|none|[custom]}`

## 12) Transforms

* **Transform (composite/toggle)**: `transform`, `transform-none`
* **Individual transforms**:
  `scale-{n}`, `scale-x-{n}`, `scale-y-{n}`;
  `rotate-{deg}`, `rotate-x-{deg}`, `rotate-y-{deg}`;
  `translate-{value}`, `translate-x-{value}`, `translate-y-{value}`, `translate-z-{value}`, `-translate-{value}`, `-translate-x-{value}`, `-translate-y-{value}`;
  `skew-x-{deg}`, `skew-y-{deg}`;
  **Transform origin**: `origin-{center|top|top-right|...}`

## 13) Interactivity

* **Accent & caret**: `accent-{color}`, `caret-{color}`
* **Appearance**: `appearance-{none|auto}`
* **Color scheme**: `color-scheme-{light|dark|only-light|only-dark|normal}`
* **Field sizing**: `field-sizing-{content|fixed}`
* **Pointer & cursor**: `pointer-events-{none|auto}`, `cursor-{pointer|wait|text|move|help|not-allowed|...}`
* **Resize**: `resize`, `resize-{x|y|none}`
* **Scroll**: `scroll-{auto|smooth}`, `scroll-m-{value}`, `scroll-m{t|r|b|l|x|y|s|e}-{value}`, `scroll-p-{value}`, `scroll-p{t|r|b|l|x|y|s|e}-{value}`
* **Scroll snap**: `snap-{none|x|y|both}`, `snap-{mandatory|proximity}`, `snap-align-{start|center|end|none}`, `snap-stop-{normal|always}`
* **Touch action**: `touch-{auto|none|pan-x|pan-y|pinch-zoom|manipulation}`
* **User select**: `select-{none|text|all|auto}`
* **Will change**: `will-change-{auto|scroll|contents|transform|[custom]}`

## 14) SVG

* **Fill**: `fill-{color}` (+ `/alpha`)
* **Stroke**: `stroke-{color}`, `stroke-{n}`
* **Stroke styling**: `stroke-{round|square|butt}`, `stroke-join-{miter|round|bevel}`, `stroke-dasharray-[...]`, `stroke-dashoffset-[...]`

## 15) Accessibility

* **Screen reader only**: `sr-only`, `not-sr-only`
* **Forced colors**: `forced-color-adjust-{auto|none}`

> **Sibling borders & rings live under other pages, but are part of core in v4:**
>
> * **Divide (sibling borders)**: `divide-x-{value}`, `divide-y-{value}` (+ `divide-{color}`, `divide-{style}`, `divide-*-reverse`) — documented under Border width.
> * **Ring utilities** are documented under Box shadow: `ring-*`, `inset-ring-*`, `ring-offset-*`.

## 16) Masks

* **Mask image**: `mask-image-{none}` or `mask-image-[url('...')]`, `mask-image-[linear-gradient(...)]` — sets `mask-image`
* **Mask size**: `mask-size-{auto|cover|contain|[length]}` — sets `mask-size`
* **Mask repeat**: `mask-repeat-{repeat|no-repeat|repeat-x|repeat-y|round|space}`, `mask-no-repeat`, `mask-repeat-x`, `mask-repeat-y`, `mask-repeat-round`, `mask-repeat-space`
* **Mask position**: `mask-position-{center|top|bottom|left|right|[position]}` — sets `mask-position`
* **Mask origin**: `mask-origin-{border|padding|content}` — maps to `border-box`, `padding-box`, `content-box`
* **Mask clip**: `mask-clip-{border|padding|content|text}` — maps to box values or `text`
* **Mask composite**: `mask-composite-{add|subtract|intersect|exclude}` — sets `mask-composite`

> All mask utilities automatically emit both standard `mask-*` and vendor-prefixed `-webkit-mask-*` declarations for cross-browser compatibility.
