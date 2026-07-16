# Ikon.Connectors.Telephony Public API

namespace Ikon.Connectors.Telephony
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
  sealed record HangupRequest
    ctor(string Reason = "")
    string Reason { get; init; }
  static class MuLawCodec
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    static byte[] Encode(ReadOnlySpan<float> samples)
  // No agent logic — the consumer supplies the brain, reading caller utterances from Turns and replying with SpeakAsync. Supports barge-in: sustained caller speech during a reply cancels the TTS. Speech detection defaults to an RMS gate; inject a custom detector for better accuracy.
  sealed class PhoneCall : IAsyncDisposable
    TimeSpan Duration { get; }
    // CallOutcome.Completed normally, or CallOutcome.Failed if the audio stream died mid-call. Calls that never connect never yield a PhoneCall.
    CallOutcome Outcome { get; }
    ValueTask DisposeAsync()
    Task HangupAsync()
    // Streams synthesized speech to the caller as 8 kHz mu-law. Returns true when the caller barged in mid-reply (stop voicing the rest); returns false immediately when text is blank or the media stream is not ready.
    Task<bool> SpeakAsync(string text, CancellationToken ct = default)
    IAsyncEnumerable<CallTurn> Turns(CancellationToken ct = default)
  // Credentials come from app.Secrets. Each placed call yields a live PhoneCall once its audio stream connects; raw, with no agent logic.
  sealed class Telephone : IAsyncDisposable
    ctor(IAppBase app, TwilioCredentials credentials, CallOptions? options = null)
    // number must be E.164. Resolves once the call's audio connects; throws CallFailedException on busy/no-answer/carrier failure, or TimeoutException if no status callback arrives within 90 seconds.
    Task<PhoneCall> CallAsync(string number, CancellationToken ct = default)
    ValueTask DisposeAsync()
  static class TelephonyAgent
    static Task<CallResult> CallAsync(AgentThread parent, Telephone telephone, string number, string objective, string personaName = "phone-callee", CancellationToken ct = default)
  static class TelephonyPersona
    static Persona Create(string name = "phone-callee", string? systemPrompt = null)
    const string DefaultName
  sealed class TelephonySkill : Skill
    ctor()
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
    const string HangupArtifact
  sealed record TwilioCredentials
    ctor(string AccountSid, string AuthToken, string FromNumber)
    string AccountSid { get; init; }
    string AuthToken { get; init; }
    string FromNumber { get; init; }
