# Ikon.Crosswind Public API

namespace Ikon.Crosswind
  sealed class CanvasDesignTokenDocument
    ctor()
    Dictionary<string, CanvasTokenValue<string>> BackdropBlur { get; init; }
    Dictionary<string, Dictionary<string, CanvasTokenValue<string>>> ColorScales { get; init; }
    CanvasEffectTokens Effects { get; init; }
    List<string> Guidelines { get; init; }
    Dictionary<string, string> Metadata { get; init; }
    string Name { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Radii { get; init; }
    CanvasSemanticColorTokens SemanticColors { get; init; }
    CanvasTypographyTokens Typography { get; init; }
    string? Version { get; init; }
    void Validate()
  static class CanvasDesignTokenLoader
    static CanvasDesignTokenDocument Load(Stream stream)
    static CanvasDesignTokenDocument Load(string json)
    static CanvasDesignTokenDocument LoadFromFile(string path)
  sealed class CanvasEffectTokens
    ctor()
    CanvasModeTokenSet BoxShadows { get; init; }
    CanvasModeTokenSet FocusRing { get; init; }
    CanvasModeTokenSet ShadowPalette { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasModeTokenSet
    ctor()
    Dictionary<string, CanvasTokenValue<string>> Dark { get; init; }
    Dictionary<string, CanvasTokenValue<string>> Light { get; init; }
    void Validate(string category)
  sealed class CanvasSemanticColorTokens
    ctor()
    CanvasModeTokenSet Background { get; init; }
    CanvasModeTokenSet Border { get; init; }
    CanvasModeTokenSet Foreground { get; init; }
    CanvasModeTokenSet Text { get; init; }
    IEnumerable<KeyValuePair<string, CanvasModeTokenSet>> Enumerate()
    void Validate()
  sealed class CanvasTokenValue<T>
    ctor()
    string? Description { get; init; }
    T Value { get; init; }
  sealed class CanvasTypographyScale
    ctor()
    string? Description { get; init; }
    string? FontFamily { get; init; }
    string FontSize { get; init; }
    string? LetterSpacing { get; init; }
    string LineHeight { get; init; }
    void Validate(string tokenName)
  sealed class CanvasTypographyTokens
    ctor()
    Dictionary<string, CanvasTypographyScale> Display { get; init; }
    Dictionary<string, CanvasTokenValue<string>> FontFamilies { get; init; }
    Dictionary<string, CanvasTokenValue<int>> FontWeights { get; init; }
    Dictionary<string, CanvasTypographyScale> Text { get; init; }
    void Validate()
  // Injectable theme data for the Flutter style resolver. Set it on a TailwindCustomStyleScope (TailwindCustomStyleScope.FlutterTheme) and pin that scope with TailwindCustomStyleRegistry.PushScope: the resolver then resolves colour scales and semantic tokens against the app's own theme instead of the hardcoded platform baseline snapshot, so custom brand themes render correctly on native clients. Lookup values may be concrete colours ("#0c0e12", "oklch(...)"), scale references ("neutral-800"), or other semantic tokens ("text-secondary"); the resolver chases references and normalizes concrete colours to hex.
  sealed class FlutterThemeSource
    ctor(IReadOnlyDictionary<string, string> scaleHex, IReadOnlyDictionary<string, string> darkSemantic, IReadOnlyDictionary<string, string> lightSemantic, double? radiusBasePx = null, IReadOnlyDictionary<string, double>? radiusPx = null, IReadOnlyDictionary<string, string>? fontFamilies = null, double? spacingUnitPx = null)
    IReadOnlyDictionary<string, string> DarkSemantic { get; }
    // Themed font families keyed by role ("body", "display", "heading", …), values are plain family names ("Fraunces") the Flutter client can load.
    IReadOnlyDictionary<string, string> FontFamilies { get; }
    IReadOnlyDictionary<string, string> LightSemantic { get; }
    // Themed radius base in logical px; rung values derive from it unless FlutterThemeSource.RadiusPx pins a rung explicitly. Null = platform default.
    double? RadiusBasePx { get; }
    // Explicit per-rung radius overrides in logical px, keyed by rung name ("lg").
    IReadOnlyDictionary<string, double> RadiusPx { get; }
    IReadOnlyDictionary<string, string> ScaleHex { get; }
    // Themed spacing unit in logical px (the density knob — web multiplies --spacing the same way). Null = platform default (4px).
    double? SpacingUnitPx { get; }
    // Builds a source from a design-token document, mapping colours only: the colour scales plus the light/dark semantic tokens. The document's radii and typography are NOT mapped — their token shapes don't line up with this type (radii are CSS strings under "radius-*" keys, not rung-named logical px; font families are quoted CSS stacks, not plain family names) — so radius, fonts, and spacing stay at platform defaults unless supplied explicitly via the constructor.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  enum TailwindColorContext
    Generic
    Background
    Foreground
    Text
    Border
  sealed class TailwindColorDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? background, IReadOnlyDictionary<string, string>? foreground, IReadOnlyDictionary<string, string>? text, IReadOnlyDictionary<string, string>? border)
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // The stock Tailwind colour palette parsed once from the authored baseline: every --color-{name}-{step} entry keyed as "{name}-{step}" → its OKLCH value. This is the single source the palette name/step views below derive from, so a token dropped from (or added to) the baseline can never silently disagree with them.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Palette family names present in the baseline (red, …, stone), first-seen order.
    static IReadOnlyList<string> PaletteNames { get; }
    // Palette steps present in the baseline (50, …, 950), ascending.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  sealed class TailwindCssVariables
    ctor(IDictionary<string, string> light, IDictionary<string, string> dark, string darkThemeName = "dark")
    IReadOnlyDictionary<string, string> Dark { get; }
    string DarkThemeName { get; }
    IReadOnlyDictionary<string, string> Light { get; }
    string EmitDark()
    string EmitLight()
  // Static facade the Crosswind compiler resolves custom aliases through. Definitions live in a TailwindCustomStyleScope that the caller pins with TailwindCustomStyleRegistry.PushScope around each compile, so several apps hosted in one process each resolve against their own theme. Lookups fall back to a process-wide scope kept for legacy single-app hosts.
  static class TailwindCustomStyleRegistry
    // Flutter theme data of the scope active for the current compile, preferring the ambient scope like the alias lookups do.
    static FlutterThemeSource? CurrentFlutterTheme { get; }
    static bool IsFontFamilyToken(string name)
    static bool IsFontWeightToken(string name)
    // Makes the given scope the ambient alias source for the current async flow until the returned handle is disposed. Compilation call sites stay static, but each caller can pin its own scope for the duration of a compile.
    static IDisposable PushScope(TailwindCustomStyleScope scope)
    static bool TryResolve(string name, TailwindColorContext context, out string value)
    static bool TryResolveFontFamily(string name, out string value)
    static bool TryResolveFontWeight(string name, out string value)
  // One isolated set of custom color and font alias definitions. Style compilation reads aliases through TailwindCustomStyleRegistry, which prefers the ambient scope pushed via TailwindCustomStyleRegistry.PushScope and falls back to the process-wide scope, so several apps hosted in one process can each compile against their own theme without contaminating the others.
  sealed class TailwindCustomStyleScope
    ctor()
    // Optional Flutter theme data derived from the same app theme as the alias definitions. The Flutter style resolver reads it through the ambient scope so each app in a shared process renders its own brand colors on native clients.
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Merges definitions into this scope. Returns true when the merge added or changed at least one alias, so callers know whether previously compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  sealed class TailwindFontDefinitions
    ctor()
    ctor(IReadOnlyDictionary<string, string>? family, IReadOnlyDictionary<string, string>? weight)
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindFontSize
    ctor(string size, string lineHeight, string? letterSpacing)
    string? LetterSpacing { get; }
    string LineHeight { get; }
    string Size { get; }
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
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
  // Target-scoping Crosswind variants. A class prefixed with flutter: applies only on the Flutter renderer, web: only on the web/CSS renderer, and an unprefixed class applies to both. This lets a single Crosswind class list carry per-target styling — e.g. ["px-3 py-2 rounded-md", "web:bg-background web:text-secondary", "flutter:bg-slate-900 flutter:text-slate-100"] — instead of maintaining a parallel token catalogue. Works with the variant-group syntax too: flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class. The marker is consumed by whichever renderer is active: the CSS compiler drops flutter: classes and strips the web: marker (emitting the class as base); the Flutter resolver drops web: classes and strips the flutter: marker.
  static class TargetVariant
    // True when variants contains the given target marker.
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns a copy of variants with the given target marker removed. The marker has been satisfied by the active renderer and must not become a CSS selector or block Flutter resolution. Returns the original reference unchanged when the marker is absent, to avoid an allocation on the common path.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web
