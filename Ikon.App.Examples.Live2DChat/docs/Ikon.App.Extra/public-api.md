# Ikon.App.Extra Public API

namespace Ikon.App.Connectors
  sealed class EmailSummary : IEquatable<EmailSummary>
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Gmail connector. Send and list mail with Google OAuth2 credentials (refresh token). Raw — the agent skill lives in Ikon.Agent.Connectors.
  sealed class Gmail
    ctor(GoogleCredentials credentials)
    // Fetch the full body of a message. Returns the text/plain part when present, falling back to the raw HTML of the text/html part (MimeKit ships no HTML-to-text converter), then to an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Stream every message matching the query, paging through the whole result set. Use a query with date operators (e.g. "after:2024/01/01") to bound a historical backfill by time.
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, CancellationToken ct = default)

namespace Ikon.App.Connectors.Browser
  // A long-lived Playwright page driven across many turns. Owns the browser lifecycle; resolves a WebTarget by mark, then accessibility role+name, then selector. Raw — no agent logic; the agent layer (Ikon.Agent.Browser) exposes these actions as tools.
  sealed class BrowserSession : IAsyncDisposable
    ctor()
    // The last ~40 console messages / page errors / failed requests from the page — the page's own account of why it is in whatever state it is in. Diagnostic gold when a page that "should" render stays blank (auth failures, websocket errors, bundle errors).
    IReadOnlyList<string> ConsoleTail { get; }
    string CurrentUrl { get; }
    ValueTask DisposeAsync()
    // Evaluate a JavaScript function-expression (e.g. "() => { ...; return 'x'; }") on the current page and return its string result. For light page-state manipulation by non-agentic callers — e.g. the codegen visual gate flipping data-theme so it can screenshot both theme states of the same view.
    Task<string?> EvaluateAsync(string script)
    Task<WebActionResult> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Screenshot as JPEG at the given quality — for callers that put the image into an LLM context, where a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  // Click the element Target resolves to, then wait for the page to settle.
  sealed class WebAction.Click : WebAction, IEquatable<WebAction.Click>
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  // Read the inner text of the element Target resolves to and record it under OutputName in the run's outputs.
  sealed class WebAction.Extract : WebAction, IEquatable<WebAction.Extract>
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  // Fill the element Target resolves to with Text. Set Secret for credentials: the live fill uses the value, but step traces and distilled flows store Fill.RedactedText in its place, so a replay must re-supply the value through its input slot rather than reusing the captured one. Set InputName to mark the value as a flow input slot that a replay substitutes.
  sealed class WebAction.Fill : WebAction, IEquatable<WebAction.Fill>
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
    // Placeholder stored anywhere a secret value would otherwise be persisted — the step trace, the distilled flow JSON, logs. Never used for the live fill.
    const string RedactedText
  // An interactable element discovered on the page, tagged for this observation.
  sealed class MarkedElement : IEquatable<MarkedElement>
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  // Go to Url and wait for the page to settle.
  sealed class WebAction.Navigate : WebAction, IEquatable<WebAction.Navigate>
    ctor(string Url)
    string Url { get; init; }
  // Press a keyboard key (e.g. "Enter", "Escape") on the focused element, then wait for the page to settle.
  sealed class WebAction.Press : WebAction, IEquatable<WebAction.Press>
    ctor(string Key)
    string Key { get; init; }
  // Scroll the page by Dx/Dy pixels (mouse wheel).
  sealed class WebAction.Scroll : WebAction, IEquatable<WebAction.Scroll>
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  // A single browser action. A tagged union so a flow serializes losslessly and replays exactly.
  abstract class WebAction : IEquatable<WebAction>
  // The result of executing one WebAction: whether it succeeded, the selector that actually resolved the target, the text an Extract produced, and a caller-actionable diagnosis when it failed.
  sealed class WebActionResult : IEquatable<WebActionResult>
    ctor(bool Ok, string Selector, string? Extracted = null, string? Failure = null)
    string? Extracted { get; init; }
    string? Failure { get; init; }
    bool Ok { get; init; }
    string Selector { get; init; }
  // A distilled, replayable integration: ordered steps with parameterized input slots.
  sealed class WebFlow : IEquatable<WebFlow>
    ctor(string Name, string Origin, IReadOnlyList<WebStep> Steps, IReadOnlyList<string> Inputs)
    IReadOnlyList<string> Inputs { get; init; }
    string Name { get; init; }
    string Origin { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
  // Turns a successful WebRun into a replayable WebFlow: keeps the steps that worked and parameterizes each filled field into a named input slot. Pure and deterministic.
  static class WebFlowDistiller
    static WebFlow Distill(WebRun run, string? name = null)
  // Deterministically replays a distilled WebFlow on a browser session — no LLM — substituting input slots with supplied values. A secret fill is redacted in the flow, so its slot must be present in inputs; the replay fails upfront when one is missing rather than filling the redaction placeholder.
  static class WebFlowPlayer
    static Task<WebReplay> ReplayAsync(BrowserSession session, WebFlow flow, IReadOnlyDictionary<string, string> inputs, CancellationToken ct = default)
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  // The result of replaying a WebFlow.
  sealed class WebReplay : IEquatable<WebReplay>
    ctor(bool Ok, IReadOnlyDictionary<string, string> Outputs, bool Healed)
    bool Healed { get; init; }
    bool Ok { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
  // The result of an operate run: outcome, summary, the action trace, and any extracted outputs. Looks counts visual inspections separately — they consume agent budget without appearing in the action trace, so budget analysis needs both numbers.
  sealed class WebRun : IEquatable<WebRun>
    ctor(WebOutcome Outcome, string Summary, IReadOnlyList<WebStep> Steps, IReadOnlyDictionary<string, string> Outputs, int Looks = 0)
    int Looks { get; init; }
    WebOutcome Outcome { get; init; }
    IReadOnlyDictionary<string, string> Outputs { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
    string Summary { get; init; }
  // One executed action, the selector that actually resolved it, and whether it succeeded. A secret Fill is stored with its value redacted at construction, so the trace — and everything derived from it (distilled flow JSON, logs) — never carries the credential.
  sealed class WebStep : IEquatable<WebStep>
    ctor(WebAction action, string resolvedSelector, bool ok)
    WebAction Action { get; init; }
    bool Ok { get; init; }
    string ResolvedSelector { get; init; }
  // How to locate an element. Prefer accessibility role + name; fall back to a CSS/XPath selector or a perception mark id from the current observation.
  sealed class WebTarget : IEquatable<WebTarget>
    ctor(string? Role = null, string? Name = null, string? Selector = null, int? Mark = null)
    int? Mark { get; init; }
    string? Name { get; init; }
    string? Role { get; init; }
    string? Selector { get; init; }

namespace Ikon.App.Connectors.Telephony
  // Thrown when an outbound call never connects — the callee was busy, did not answer, or the carrier rejected/canceled the call. CallFailedException.Outcome carries the specific fate.
  sealed class CallFailedException : Exception
    ctor(CallOutcome outcome, string message)
    CallOutcome Outcome { get; }
  // Raw call tuning: the TTS voice, spoken language, and a hard duration cap. Model/agent choices live in the agent layer (Ikon.Agent.Telephony), not here.
  sealed class CallOptions : IEquatable<CallOptions>
    ctor(string VoiceId = "", string Language = "en-US", TimeSpan? MaxDuration = null)
    // Spoken language of the call.
    string Language { get; init; }
    // Hard cap on call length; null means the 10 minute default.
    TimeSpan? MaxDuration { get; init; }
    // TTS voice for spoken replies; empty uses the speech generator's default.
    string VoiceId { get; init; }
  enum CallOutcome
    Completed
    NoAnswer
    Busy
    Failed
  sealed class CallResult : IEquatable<CallResult>
    ctor(string Transcript, CallOutcome Outcome, TimeSpan Duration)
    TimeSpan Duration { get; init; }
    CallOutcome Outcome { get; init; }
    string Transcript { get; init; }
  // A completed caller utterance: its transcript plus the raw mu-law audio.
  sealed class CallTurn : IEquatable<CallTurn>
    ctor(string Transcript, byte[] AudioMuLaw)
    byte[] AudioMuLaw { get; init; }
    string Transcript { get; init; }
  // G.711 mu-law codec for telephony audio (8-bit, 8kHz), the encoding Twilio Media Streams uses on the wire. Converts between mu-law bytes and normalized float samples.
  static class MuLawCodec
    // Decodes mu-law bytes to float samples normalized to [-1.0, 1.0].
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    // Encodes float samples (normalized to [-1.0, 1.0]) to mu-law bytes.
    static byte[] Encode(ReadOnlySpan<float> samples)
  // A live phone call — the real-time audio engine. Segments caller speech into turns (PhoneCall.Turns), speaks replies (PhoneCall.SpeakAsync), and hangs up. No agent logic: the brain is supplied by the consumer (Ikon.Agent.Connectors.Telephony binds a call to a subthread). Supports barge-in: sustained caller speech during a reply cancels TTS and flushes Twilio's buffer. Speech detection uses Silero VAD (falls back to an RMS gate if the model can't load).
  sealed class PhoneCall : IAsyncDisposable
    TimeSpan Duration { get; }
    // How the connected call ended: Completed normally, Failed when the audio stream died mid-call. Calls that never connect (busy, no answer, carrier failure) never yield a PhoneCall — they surface as CallFailedException from Telephone.CallAsync.
    CallOutcome Outcome { get; }
    ValueTask DisposeAsync()
    Task HangupAsync()
    // Speak a reply to the caller (TTS → 8kHz mu-law → Media Streams). Interruptible by barge-in; returns true if the caller barged in (so the consumer can stop voicing the rest of the reply).
    Task<bool> SpeakAsync(string text, CancellationToken ct = default)
    // Caller utterances as they complete, until the call ends.
    IAsyncEnumerable<CallTurn> Turns(CancellationToken ct = default)
  // Silero VAD (voice activity detection) over ONNX Runtime — fast (~1-2ms) speech detection, far more robust to line noise / TTS echo than a plain RMS gate. Used for barge-in and utterance segmentation. Ported from the Nanobot voice prototype.
  sealed class SileroVad : IDisposable
    float Threshold { get; set; }
    bool ContainsSpeech(float[] samples)
    // Create from the embedded ONNX model. Returns null (and logs) if it can't load.
    static SileroVad? CreateFromEmbeddedResource(int sampleRate = 16000, Action<string>? log = null)
    void Dispose()
    float GetSpeechProbability(float[] samples)
    // Reset model state when starting a new audio stream.
    void Reset()
  // Places outbound Twilio calls and hosts the Media Streams WebSocket. Each placed call yields a live PhoneCall once the audio stream connects. Raw — no agent logic; credentials come from app.Secrets.
  sealed class Telephone : IAsyncDisposable
    ctor(IAppBase app, TwilioCredentials credentials, CallOptions? options = null)
    // Place a call to an E.164 number; resolves to the live call once audio connects.
    Task<PhoneCall> CallAsync(string number, CancellationToken ct = default)
    ValueTask DisposeAsync()
  // Twilio credentials. Supplied from app.Secrets at construction; never hardcoded.
  sealed class TwilioCredentials : IEquatable<TwilioCredentials>
    ctor(string AccountSid, string AuthToken, string FromNumber)
    string AccountSid { get; init; }
    string AuthToken { get; init; }
    string FromNumber { get; init; }
