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
    static CanvasDesignTokenDocument LoadFromFile(string path)
    static CanvasDesignTokenDocument LoadFromJson(string json)
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
    required T Value { get; init; }
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
  // To take effect, assign an instance to TailwindCustomStyleScope.FlutterTheme and pin that scope via TailwindCustomStyleRegistry.PushScope; the resolver then resolves colour scales and semantic tokens against it instead of the platform baseline. Lookup values may be concrete colours, scale references ("neutral-800"), or other semantic tokens — the resolver chases references and normalizes concrete colours to hex. Construct with the object-initializer form, which names each map (new FlutterThemeSource { ScaleColors = …, LightSemantic = …, DarkSemantic = … }); ScaleColors, LightSemantic, and DarkSemantic share a dictionary type, so a positional form would let a transposition of the light and dark maps compile and silently invert the two modes. Each unset map defaults to empty.
  sealed class FlutterThemeSource
    ctor()
    // Dark-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are raw colour strings, scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> DarkSemantic { get; init; }
    // Keyed by role ("body", "display", "heading", …); values are plain family names ("Fraunces"), not CSS font stacks.
    IReadOnlyDictionary<string, string> FontFamilies { get; init; }
    // Light-mode semantic tokens keyed by "prefix-name" ("bg-surface"). Values are raw colour strings, scale references ("neutral-800"), or other semantic tokens — copied verbatim from the tokens, so not necessarily hex.
    IReadOnlyDictionary<string, string> LightSemantic { get; init; }
    // Logical px. Rung values derive from this unless RadiusPx pins a rung explicitly; null means platform default.
    double? RadiusBasePx { get; init; }
    // Values are logical px, keyed by rung name (e.g. "lg"); a pinned rung overrides the value derived from RadiusBasePx.
    IReadOnlyDictionary<string, double> RadiusPx { get; init; }
    // Colour-scale entries keyed by "scale-shade" ("neutral-800"). Values are the raw colour strings copied verbatim from the tokens — they may be oklch(...), #rrggbb, or any other CSS colour form, not necessarily hex.
    IReadOnlyDictionary<string, string> ScaleColors { get; init; }
    // Logical px per spacing unit; scales every numeric spacing utility. Null means platform default (4px).
    double? SpacingUnitPx { get; init; }
    // Maps colours only (colour scales plus light/dark semantic tokens). Radii, typography, and spacing are NOT mapped and stay at platform defaults unless supplied via the object initializer.
    static FlutterThemeSource FromDesignTokens(CanvasDesignTokenDocument document)
  enum TailwindColorContext
    // Untyped context (rings, shadows, gradients). The only context that falls back to the union of all aliases — background, foreground, text, and border merged — when the name is not found in a family-scoped map.
    Generic
    // Family-scoped to background aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Background
    // Family-scoped to foreground aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Foreground
    // Family-scoped to text aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Text
    // Family-scoped to border aliases only. An alias defined under another context does not resolve here; unlike Generic, it does not fall back to the merged union.
    Border
  // Custom colour alias maps split by role. Construct with the object-initializer form, which names each property (new TailwindColorDefinitions { Background = …, Text = … }); the four maps share a dictionary type, so a positional form would let a transposition of any two compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindColorDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Background { get; init; }
    IReadOnlyDictionary<string, string> Border { get; init; }
    IReadOnlyDictionary<string, string> Foreground { get; init; }
    IReadOnlyDictionary<string, string> Text { get; init; }
    void Validate()
  static class TailwindCssBaseline
    static string AdditionalCss { get; }
    // Intentionally empty. Tailwind's stock palette has no separate dark root-variable set — dark mode is expressed through utility classes, not a second baseline — so there is nothing to parse here. A theme's dark appearance comes entirely from the dark overrides the app passes to TailwindCssVariables; those are merged onto this empty base, so an app that emits dark CSS must supply its own dark values rather than expecting a baseline to fall back on.
    static IReadOnlyDictionary<string, string> DarkVariables { get; }
    static IReadOnlyDictionary<string, string> LightVariables { get; }
    // Keyed "{name}-{step}" (e.g. "red-50") → OKLCH value.
    static IReadOnlyDictionary<string, string> PaletteColors { get; }
    // Ordered as first seen in the baseline.
    static IReadOnlyList<string> PaletteNames { get; }
    // Ascending numeric order.
    static IReadOnlyList<string> PaletteSteps { get; }
    static string GetFullBaseline()
  // Light and dark CSS variable maps for a compiled theme, each merged over the Tailwind baseline. Construct with the object-initializer form, which names each map (new TailwindCssVariables { Light = …, Dark = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently invert the emitted light/dark CSS. An omitted map defaults to the baseline alone.
  sealed class TailwindCssVariables
    ctor()
    // CSS variables for the dark theme, merged over the Tailwind dark baseline.
    IReadOnlyDictionary<string, string> Dark { get; init; }
    // Theme name the dark variables are emitted under.
    string DarkThemeName { get; init; }
    // CSS variables for the light theme, merged over the Tailwind light baseline.
    IReadOnlyDictionary<string, string> Light { get; init; }
    string EmitDark()
    string EmitLight()
  // Pin a TailwindCustomStyleScope with PushScope around each compile; lookups prefer the ambient scope and fall back to a process-wide scope for legacy single-app hosts.
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
  // Compilation resolves aliases against the ambient scope pinned by TailwindCustomStyleRegistry.PushScope, falling back to the process-wide scope; pin an instance around a compile so co-hosted apps stay isolated.
  sealed class TailwindCustomStyleScope
    ctor()
    // Optional Flutter theme data derived from the same app theme as the alias definitions. The Flutter style resolver reads it through the ambient scope so each app in a shared process renders its own brand colors on native clients.
    FlutterThemeSource? FlutterTheme { get; set; }
    bool IsFontFamilyToken(string name)
    bool IsFontWeightToken(string name)
    // Returns true when the merge added or changed at least one alias — the signal that already-compiled styles may now resolve differently and need recompilation.
    bool MergeDefinitions(TailwindStyleDefinitions definitions)
    void SetDefinitions(TailwindStyleDefinitions? definitions)
    bool TryResolve(string name, TailwindColorContext context, out string value)
    bool TryResolveFontFamily(string name, out string value)
    bool TryResolveFontWeight(string name, out string value)
  // Custom font family and weight alias maps. Construct with the object-initializer form, which names each property (new TailwindFontDefinitions { Family = …, Weight = … }); the two maps share a dictionary type, so a positional form would let a transposition compile and silently mis-map the roles. An omitted map defaults to empty.
  sealed class TailwindFontDefinitions
    ctor()
    IReadOnlyDictionary<string, string> Family { get; init; }
    IReadOnlyDictionary<string, string> Weight { get; init; }
    void Validate()
  sealed class TailwindStyleDefinitions
    ctor()
    ctor(TailwindColorDefinitions colors, TailwindFontDefinitions? fonts = null)
    TailwindColorDefinitions Colors { get; init; }
    TailwindFontDefinitions Fonts { get; init; }
    void Validate()
  // flutter:-prefixed classes apply only on the Flutter renderer, web: only on web/CSS, unprefixed on both; the active renderer strips its own marker and drops the other's classes. Variant-group syntax flutter:(bg-slate-900 text-slate-100) applies the marker to every grouped class.
  static class TargetVariant
    // True when variants contains the given target marker.
    static bool Has(IReadOnlyList<string> variants, string target)
    // Returns the same reference (no copy) when the marker is absent.
    static IReadOnlyList<string> Without(IReadOnlyList<string> variants, string target)
    const string Flutter
    const string Web
