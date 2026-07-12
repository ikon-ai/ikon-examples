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
    // Fetch the full plain-text body of a message. Returns the text/plain part when present, falling back to the text extracted from the HTML part, then to an empty string.
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
    Task<(bool Ok, string Selector, string? Extracted, string? Failure)> ExecuteAsync(WebAction action)
    Task<IReadOnlyList<MarkedElement>> MarkElementsAsync()
    Task NavigateAsync(string url)
    Task<byte[]> ScreenshotAsync()
    // Screenshot as JPEG at the given quality — for callers that put the image into an LLM context, where a PNG's 3-5x larger payload rides along for every later turn.
    Task<byte[]> ScreenshotJpegAsync(int quality = 70)
    Task StartAsync(bool headless, bool captureGrade = false, CancellationToken ct = default)
  sealed class WebAction.Click : WebAction, IEquatable<WebAction.Click>
    ctor(WebTarget Target)
    WebTarget Target { get; init; }
  sealed class WebAction.Extract : WebAction, IEquatable<WebAction.Extract>
    ctor(WebTarget Target, string OutputName)
    string OutputName { get; init; }
    WebTarget Target { get; init; }
  sealed class WebAction.Fill : WebAction, IEquatable<WebAction.Fill>
    ctor(WebTarget Target, string Text, bool Secret = false, string? InputName = null)
    string? InputName { get; init; }
    bool Secret { get; init; }
    WebTarget Target { get; init; }
    string Text { get; init; }
  // An interactable element discovered on the page, tagged for this observation.
  sealed class MarkedElement : IEquatable<MarkedElement>
    ctor(int Mark, string Role, string Name, string Selector)
    int Mark { get; init; }
    string Name { get; init; }
    string Role { get; init; }
    string Selector { get; init; }
  sealed class WebAction.Navigate : WebAction, IEquatable<WebAction.Navigate>
    ctor(string Url)
    string Url { get; init; }
  sealed class WebAction.Press : WebAction, IEquatable<WebAction.Press>
    ctor(string Key)
    string Key { get; init; }
  sealed class WebAction.Scroll : WebAction, IEquatable<WebAction.Scroll>
    ctor(int Dx, int Dy)
    int Dx { get; init; }
    int Dy { get; init; }
  // A single browser action. A tagged union so a flow serializes losslessly and replays exactly.
  abstract class WebAction : IEquatable<WebAction>
  // A distilled, replayable integration: ordered steps with parameterized input slots.
  sealed class WebFlow : IEquatable<WebFlow>
    ctor(string Name, string Origin, IReadOnlyList<WebStep> Steps, IReadOnlyList<string> Inputs)
    IReadOnlyList<string> Inputs { get; init; }
    string Name { get; init; }
    string Origin { get; init; }
    IReadOnlyList<WebStep> Steps { get; init; }
  // Turns a successful WebRun into a replayable WebFlow : keeps the steps that worked and parameterizes each filled field into a named input slot. Pure and deterministic.
  static class WebFlowDistiller
    static WebFlow Distill(WebRun run, string? name = null)
  // Deterministically replays a distilled WebFlow on a browser session — no LLM — substituting input slots with supplied values.
  static class WebFlowPlayer
    static Task<WebReplay> ReplayAsync(BrowserSession session, WebFlow flow, IReadOnlyDictionary<string, string> inputs, CancellationToken ct = default)
  enum WebOutcome
    Succeeded
    Failed
    BudgetExhausted
  // The result of replaying a WebFlow .
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
  // One executed action, the selector that actually resolved it, and whether it succeeded.
  sealed class WebStep : IEquatable<WebStep>
    ctor(WebAction Action, string ResolvedSelector, bool Ok)
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
  // Raw call tuning: the TTS voice, spoken language, and a hard duration cap. Model/agent choices live in the agent layer (Ikon.Agent.Telephony), not here.
  sealed class CallOptions : IEquatable<CallOptions>
    ctor(string VoiceId = "", string Language = "en-US", TimeSpan? MaxDuration = null)
    string Language { get; init; }
    TimeSpan? MaxDuration { get; init; }
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
  // A live phone call — the real-time audio engine. Segments caller speech into turns ( Turns ), speaks replies ( SpeakAsync ), and hangs up. No agent logic: the brain is supplied by the consumer (Ikon.Agent.Connectors.Telephony binds a call to a subthread). Supports barge-in: sustained caller speech during a reply cancels TTS and flushes Twilio's buffer. Speech detection uses Silero VAD (falls back to an RMS gate if the model can't load).
  sealed class PhoneCall : IAsyncDisposable
    TimeSpan Duration { get; }
    CallOutcome Outcome { get; }
    ValueTask DisposeAsync()
    Task HangupAsync()
    // Speak a reply to the caller (TTS → 8kHz mu-law → Media Streams). Interruptible by barge-in; returns true if the caller barged in (so the consumer can stop voicing the rest of the reply).
    Task<bool> SpeakAsync(string text, CancellationToken ct = default)
    // Caller utterances as they complete, until the call ends.
    IAsyncEnumerable<CallTurn> Turns(CancellationToken ct = default)
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
