namespace Ikon.Connectors.Browser
  static class BrowserOperatorPersona
    static Persona Create(string name = "browser-operator", string? systemPrompt = null, LLMModel visionModel = Claude46Sonnet, Reasoning? reasoning = null)
    const string DefaultName
  // Owns the browser lifecycle: start once, dispose to release the process. Resolves a WebTarget by mark first, then accessibility role+name, then selector.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    // The last ~40 console messages, page errors, and failed requests from the page — the page's own account of why it is in whatever state it is in. Check it when a page that should render stays blank (auth failures, websocket errors, bundle errors).
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    Task AddInitScriptAsync(string script)
    ValueTask DisposeAsync()
    // script is a JavaScript function-expression (e.g. "() => { ...; return 'x'; }"); the result is returned as a string.
    Task<string?> EvaluateAsync(string script)
    Task<WebActionResult> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Prefer this over ScreenshotAsync when the image enters an LLM context — a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    // Call once; throws InvalidOperationException if already started (dispose first). captureGrade renders at a 1440×900 2× viewport for high-fidelity single-shot screenshots — leave false for interactive driving, where the larger payload is pure token cost.
    // headless: Run the browser without a visible window.
    // captureGrade: High-fidelity capture mode for single-shot visual grading: 1440×900 viewport at 2x device scale, so small text, hairline borders, and gradients survive to the vision model. Leave false for agentic driving sessions — their screenshots ride along in every later LLM turn, where the 4x pixel payload is pure token cost.
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  sealed record MarkedElement
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  abstract record WebAction
  sealed record WebAction.Click : WebAction
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  sealed record WebAction.Extract : WebAction
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  sealed record WebAction.Fill : WebAction
    // Secret: Set for credentials: the live fill uses the value, but step traces and distilled flows store RedactedText in its place, so a replay must re-supply the value through its input slot rather than reusing the captured one.
    // InputName: Marks the value as a flow input slot that a replay substitutes.
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
    // Key: A key name such as "Enter" or "Escape".
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
  // The persona named personaName must be registered on the orchestrator — build it with BrowserOperatorPersona.Create.
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
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  sealed record WebReplay
    // Healed: Reserved for self-healing replay, which is not yet implemented — this is currently always false, so do not branch on it expecting a meaningful value.
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  sealed record WebRun
    // Looks: Count of visual inspections — they consume agent budget without appearing in Steps, so budget analysis needs both numbers.
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
  // Resolution tries the perception mark id first, then accessibility role + name, then a CSS/XPath selector — populate whichever are known, since the later ones are what let a replay still find the element once the marks have gone stale.
  sealed record WebTarget
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }
