# Ikon.Connectors.Browser Public API

namespace Ikon.Connectors.Browser
  static class BrowserOperatorPersona
    static Persona Create(string name = "browser-operator", string? systemPrompt = null, LLMModel visionModel = Claude46Sonnet, Reasoning? reasoning = null)
    const string DefaultName
  // Owns the browser lifecycle: start once, dispose to release the process. Resolves a WebTarget by mark first, then accessibility role+name, then selector.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    ValueTask DisposeAsync()
    Task<string?> EvaluateAsync(string script)
    Task<WebActionResult> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    // Call once; throws InvalidOperationException if already started (dispose first). captureGrade renders at a 1440×900 2× viewport for high-fidelity single-shot screenshots — leave false for interactive driving, where the larger payload is pure token cost.
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  sealed class BrowserSkill : Skill
    ctor(LLMModel visionModel = Claude46Sonnet)
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  sealed record ClickRequest
    ctor(int Mark)
    int Mark { get; init; }
  sealed record ExtractRequest
    ctor(int Mark, string OutputName)
    int Mark { get; init; }
    string OutputName { get; init; }
  sealed record FillRequest
    ctor(int Mark, string Text)
    int Mark { get; init; }
    string Text { get; init; }
  sealed record FinishRequest
    ctor(bool Success, string Summary)
    bool Success { get; init; }
    string Summary { get; init; }
  sealed record LookRequest
    ctor(string? Screenshot = null)
    string? Screenshot { get; init; }
  sealed record MarkedElement
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  sealed record NavigateRequest
    ctor(string Url)
    string Url { get; init; }
  sealed record ScrollRequest
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  abstract record WebAction
  sealed record WebAction.Click : WebAction
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  sealed record WebAction.Extract : WebAction
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  sealed record WebAction.Fill : WebAction
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
    const string RedactedText
  sealed record WebAction.Navigate : WebAction
    ctor(string Url)
    string Url { get; init; }
  sealed record WebAction.Press : WebAction
    ctor(string Key)
    string Key { get; init; }
  sealed record WebAction.Scroll : WebAction
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  sealed record WebActionResult
    ctor(bool Ok, string Selector, string? Extracted = null, string? Failure = null)
    string? Extracted { get; init; }
    string? Failure { get; init; }
    bool Ok { get; init; }
    string Selector { get; init; }
  static class WebAgent
    static WebFlow Distill(WebRun run, string? name = null)
    static Task<WebRun> OperateAsync(AgentThread parent, string url, string objective, WebAgentOptions? options = null, string personaName = "browser-operator", CancellationToken ct = default)
    static Task<WebReplay> ReplayAsync(WebFlow flow, IReadOnlyDictionary<string, string> inputs, bool headless = true, CancellationToken ct = default)
  sealed record WebAgentOptions
    ctor(int MaxSteps = 25, bool Headless = true)
    bool Headless { get; init; }
    int MaxSteps { get; init; }
  sealed record WebFlow
    ctor(string Name, string Origin, IReadOnlyList<WebStep> Steps, IReadOnlyList<string> Inputs)
    IReadOnlyList<string> Inputs { get; init; }
    string Name { get; init; }
    string Origin { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
  // Keeps only the steps that succeeded and parameterizes each filled field into a named input slot. Deterministic; secret fills are redacted in the produced WebFlow.
  static class WebFlowDistiller
    static WebFlow Distill(WebRun run, string? name = null)
  // Replays a distilled WebFlow deterministically (no LLM), substituting each input slot from inputs. A secret fill's slot must be supplied — a missing one fails upfront rather than typing the redaction placeholder.
  static class WebFlowPlayer
    static Task<WebReplay> ReplayAsync(BrowserSession session, WebFlow flow, IReadOnlyDictionary<string, string> inputs, CancellationToken ct = default)
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  sealed record WebReplay
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  sealed record WebRun
    ctor(WebOutcome Outcome, string Summary, IReadOnlyList<WebStep> Steps, IReadOnlyDictionary<string, string> Outputs, int Looks = 0)
    int Looks { get; init; }
    WebOutcome Outcome { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
    string Summary { get; init; }
  sealed record WebStep
    ctor(WebAction action, string resolvedSelector, bool ok)
    WebAction Action { get; init; }
    bool Ok { get; init; }
    string ResolvedSelector { get; init; }
  sealed record WebTarget
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }
