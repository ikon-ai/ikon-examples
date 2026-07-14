# Ikon.App.Extra Public API

namespace Ikon.App.Connectors
  sealed record EmailSummary
    ctor(string Id, string ThreadId, string From, string Subject, string Snippet, DateTimeOffset ReceivedAt)
    string From { get; init; }
    string Id { get; init; }
    DateTimeOffset ReceivedAt { get; init; }
    string Snippet { get; init; }
    string Subject { get; init; }
    string ThreadId { get; init; }
  // Authenticates with Google OAuth2 (refresh-token) credentials. Raw connector — no agent logic.
  sealed class Gmail
    ctor(GoogleCredentials credentials)
    // Returns the text/plain part when present, else the raw HTML of the text/html part, else an empty string.
    Task<string> GetBodyAsync(string id, CancellationToken ct = default)
    // Pages through the entire result set, unlike ListAsync which is capped by its limit. Bound a historical backfill with query date operators, e.g. "after:2024/01/01".
    IAsyncEnumerable<EmailSummary> ListAllAsync(string? query = null, CancellationToken ct = default)
    Task<IReadOnlyList<EmailSummary>> ListAsync(string? query = null, int limit = 20, CancellationToken ct = default)
    Task<string> SendAsync(string to, string subject, string body, string? cc = null, CancellationToken ct = default)

namespace Ikon.App.Connectors.Browser
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

namespace Ikon.App.Connectors.Telephony
  // Thrown when an outbound call never connects (busy, no answer, or carrier rejection); Outcome carries the specific fate.
  sealed class CallFailedException : Exception
    ctor(CallOutcome outcome, string message)
    CallOutcome Outcome { get; }
  // Empty VoiceId uses the speech generator's default voice; null MaxDuration caps the call at 10 minutes.
  sealed record CallOptions
    ctor(string VoiceId = "", string Language = "en-US", TimeSpan? MaxDuration = null)
    string Language { get; init; }
    TimeSpan? MaxDuration { get; init; }
    string VoiceId { get; init; }
  enum CallOutcome
    Completed
    NoAnswer
    Busy
    Failed
  sealed record CallResult
    ctor(string Transcript, CallOutcome Outcome, TimeSpan Duration)
    TimeSpan Duration { get; init; }
    CallOutcome Outcome { get; init; }
    string Transcript { get; init; }
  sealed record CallTurn
    ctor(string Transcript, byte[] AudioMuLaw)
    byte[] AudioMuLaw { get; init; }
    string Transcript { get; init; }
  static class MuLawCodec
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    static byte[] Encode(ReadOnlySpan<float> samples)
  // No agent logic — the consumer supplies the brain, reading caller utterances from Turns and replying with SpeakAsync. Supports barge-in: sustained caller speech during a reply cancels the TTS. Speech detection uses Silero VAD, falling back to an RMS gate if the model can't load.
  sealed class PhoneCall : IAsyncDisposable
    TimeSpan Duration { get; }
    // CallOutcome.Completed normally, or CallOutcome.Failed if the audio stream died mid-call. Calls that never connect never yield a PhoneCall.
    CallOutcome Outcome { get; }
    ValueTask DisposeAsync()
    Task HangupAsync()
    // Streams synthesized speech to the caller as 8 kHz mu-law. Returns true when the caller barged in mid-reply (stop voicing the rest); returns false immediately when text is blank or the media stream is not ready.
    Task<bool> SpeakAsync(string text, CancellationToken ct = default)
    IAsyncEnumerable<CallTurn> Turns(CancellationToken ct = default)
  sealed class SileroVad : IDisposable
    float Threshold { get; set; }
    bool ContainsSpeech(float[] samples)
    static SileroVad? CreateFromEmbeddedResource(int sampleRate = 16000, Action<string>? log = null)
    void Dispose()
    float GetSpeechProbability(float[] samples)
    void Reset()
  // Credentials come from app.Secrets. Each placed call yields a live PhoneCall once its audio stream connects; raw, with no agent logic.
  sealed class Telephone : IAsyncDisposable
    ctor(IAppBase app, TwilioCredentials credentials, CallOptions? options = null)
    // number must be E.164. Resolves once the call's audio connects; throws CallFailedException on busy/no-answer/carrier failure, or TimeoutException if no status callback arrives within 90 seconds.
    Task<PhoneCall> CallAsync(string number, CancellationToken ct = default)
    ValueTask DisposeAsync()
  sealed record TwilioCredentials
    ctor(string AccountSid, string AuthToken, string FromNumber)
    string AccountSid { get; init; }
    string AuthToken { get; init; }
    string FromNumber { get; init; }
