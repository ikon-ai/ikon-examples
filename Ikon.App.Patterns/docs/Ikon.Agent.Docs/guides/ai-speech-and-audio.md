# AI Speech & Audio

## AI Speech & Audio

Text-to-speech with `Audio.SpeakAsync(text)`, speech-to-text with `SpeechRecognizer.RecognizeAsync(samples, sampleRate)`, and sound effects with `SoundEffectGenerator.GenerateAsync(prompt)`. Audio playback via `Audio.SendSpeech()`.

`Audio` is an app service initialized in your app class: `private Audio Audio { get; } = new(app);`

### Speech Generation (TTS)

```csharp
// Generate speech and play it to clients — one call. A new call fades out and
// replaces whatever is still playing (the interrupt behavior a voice app wants).
// Name a voice that fits the product: the bare default ("Aria") is a mature, hard read
// that suits few apps — "Sarah" is a softer, modern one to reach for. Other voices:
// Jessica, Lily, Matilda, Charlotte (female); George, Brian, Will (male).
await Audio.SpeakAsync("Hello world", voice: "Sarah");

// Pick a model, shape the delivery, or target specific clients:
await Audio.SpeakAsync("Hello world", SpeechGeneratorModel.Eleven3, voice: "Sarah",
    instructions: "Soft and warm, almost a whisper", speed: 0.96, targetIds: [clientSessionId]);  // speed is a double, 1.0 = normal
```

To get the audio WITHOUT playing it (e.g. to store or post-process a clip), use the one-shot `SpeechGenerator.GenerateAsync(text)` — it returns a single PCM `AudioChunk` (never null; throws `AIException` on failure):

```csharp
var audio = await SpeechGenerator.GenerateAsync("Hello world");  // ElevenFlash25 (cheap+fast) by default
// audio.Samples (float[]), audio.SampleRate, audio.ChannelCount
```

Hand-roll the generator loop only when you need custom mixing, speech that must not interrupt what is playing, chunk-by-chunk streaming, or config beyond text, voice, instructions, and speed (e.g. language):

```csharp
using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hei maailma", Language = "fi" }))
{
    Audio.SendSpeech(audio);  // Audio is an app service property
}
```

### Speech Recognition (STT)

For push-to-talk → chat (the most common case), use `PushToTalkButton` and `Audio.SpeechRecognizedAsync`. The framework wires up audio capture, transcription, and routing for you — no streamId-to-client plumbing.

```csharp
// One-time setup in the app
Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

Audio.SpeechRecognizedAsync += async args =>
{
    // args.Text — recognized speech
    // args.ClientSessionId / args.UserId — who said it
    // ClientScope is established automatically — per-client reactive writes route correctly.
    await SendChatMessageAsync(args.Text);
};

// In your UI lambda:
view.PushToTalkButton(style: ["w-16 h-16 rounded-full bg-red-600"]);
```

#### Timestamps, speakers and confidence

`RecognizeBatchSpeechAsync` returns a `Transcript` — `Text`, `Language`, `Duration`, `Confidence` —
not a string, and `RecognizeContinuousSpeechAsync` yields `TranscriptEvent`. Ask for timings with
`Timestamps`; they are off by default, so an unchanged request costs what it always did.

```csharp
using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);

var transcript = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
{
    Samples = samples,
    SampleRate = 16000,
    ChannelCount = 1,
    Timestamps = SpeechTimestamps.Word,
});

foreach (var word in transcript.Words)   // SpeechWord: Text, Start, End, Confidence, Speaker
{
    Log.Instance.Info($"[{word.Start.TotalSeconds:F2}] {word.Text}");
}
```

`Words` and `Segments` (`TranscriptSegment`) are `TimeSpan` offsets from the start of the submitted
audio, normalised from whatever unit the provider reported. Both are empty unless asked for.

**Asking for a granularity the model does not support throws** — it does not return an empty list,
because an empty `Words` would otherwise mean both "not supported" and "no speech". Check
`SpeechRecognizer.GetCapabilities(model)` (`SupportsWordTimestamps`, `SupportsSegmentTimestamps`,
`SupportsDiarization`) before asking. Two provider limits surface as exceptions rather than as
quietly missing fields: the GPT-4o transcribe models have no timings at all (the diarizing variant
reports speaker segments but never words), and Voxtral takes **one** granularity per request and
rejects a language hint together with timestamps.

For the app-level events, pass `timestamps:` to `UseSpeechRecognition` / `UseTurnDetection` and read
`args.Transcript`; `args.Text` is unchanged. Continuous recognition only puts words on events where
`IsFinal` is true — no provider attaches word timings to an interim hypothesis — and interim events
arrive only when `InterimResults` asks for them.

`MicToggleButton` is the tap-to-talk twin: tap opens the microphone, tap again closes it, and the segment in between is transcribed exactly like a PushToTalkButton hold. Prefer it when holding a button is impractical (mobile, hands-busy, long dictation). Pick ONE for a given microphone — offering both hold and toggle for the same mic is the ambiguity users report as "is it on?".

```csharp
view.MicToggleButton();
```

**Mic button UX rules (important — a mic that gives no feedback reads as broken):**

- **The microphone permission is the button's job, and it is a SEPARATE press.** Until the browser has granted a mic, both buttons render themselves as an "Enable microphone" pill and a press only asks — it never also starts a capture. Do not build your own permission flow, and do not defeat this one. The reason it is separate: a permission dialog takes focus, which the page reads as the button being released, so a hold that doubles as the ask is cancelled behind the dialog and captures nothing. The user grants, comes back to an idle-looking button, and reasonably concludes the app is broken. After a grant the button flashes a green "ready" ring for two seconds, so the answer to "is it on now?" is on screen before it is asked.
- **NEVER put a mic button behind `disabled:` for permission reasons.** A disabled button cannot ask, so the user has no way out of the state. `disabled:` means "the app is busy", nothing else.
- **Handle a refusal.** `onPermissionChanged` fires with the answer; when it is not `MediaPermissionState.Granted`, offer typing instead or say where the browser's site settings are. The button itself switches to a "Microphone blocked" state and stays pressable so it can explain itself.
- **The button must visibly change while the mic is open.** Both buttons ship a themed default (`MicButton.Default`) that already does this, plus the whole state sequence. With no style array you get correct UX for free.
- **Feedback must be zero-latency.** Every state keys on the client-stamped `data-ikon-capture-state` attribute (`idle`, `pressed`, `live`, `ready`, `prompt`, `requesting`, `denied`, `unavailable`), so it lands in the frame of the press — including `pressed`, which fires before the mic has finished opening. If you pass a custom style array it REPLACES the default (opt-in rule), so re-add the states: include the `MicButton.States` token (`[MicButton.States, "your classes"]`) or write your own `data-[ikon-capture-state=*]:*` variants. Never mirror capture state into a `ClientReactive<bool>` from `onCaptureStart` — that is a server round trip per press and it is visibly late.
- **Make the interaction model obvious.** Label a PushToTalkButton with a hold affordance ("Hold to talk") and a MicToggleButton with a tap affordance ("Tap to talk") — users cannot tell hold from toggle by looking at a bare mic icon.
- **Mobile: make it big and gesture-proof.** Comfortable touch target (h-12+); the themed default already includes `touch-none select-none` so scroll pans and text selection can't break the hold — keep those classes in custom styles.
- A toggle mic that is open MUST stay visibly red for as long as it is open — an invisible open microphone is recording the user without them knowing.

**Important — do NOT plumb state from `onCaptureStart` into audio frame handlers.** The audio events (`AudioInputStreamBeginAsync`, `AudioInputFrameAsync`) already carry `args.ClientContext` / `args.ClientSessionId`. A pattern like `onCaptureStart: e => _streamToClient[e.StreamId] = clientId` is unnecessary — read `args.ClientSessionId` directly inside the audio handler.

#### Continuous / silence-triggered recognition (advanced)

For always-on transcription with VAD-style segmentation (e.g., a virtual assistant that listens continuously), drop down to `SpeechRecognizerAdapter`:

```csharp
var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
var adapter = new SpeechRecognizerAdapter(recognizer, new SpeechRecognizerAdapter.Config
{
    Mode = SpeechRecognizerAdapter.Mode.SilenceTriggered,
    SilenceDuration = TimeSpan.FromMilliseconds(750),
    SilenceThreshold = 0.01f,
    MaxSpeechDuration = TimeSpan.FromSeconds(30)
});
```

#### Custom raw audio handling (advanced)

If you need direct access to PCM samples (e.g., custom DSP, your own VAD), subscribe to the audio events and **register per-stream state inside `AudioInputStreamBeginAsync` using `args.ClientSessionId`** — that fires reliably before any frame handler observes a frame from the stream. To transcribe samples you already hold, use the one-shot `var text = await SpeechRecognizer.RecognizeAsync(samples, sampleRate);` (WhisperLarge3Turbo, cheap+fast, by default) — that one still returns a plain string. Reach for `RecognizeBatchSpeechAsync` when you want the `Transcript` with timings.

```csharp
Audio.AudioInputStreamBeginAsync += async args =>
{
    // Snapshot per-stream state here. args.ClientSessionId / args.UserId identify the client.
    _myStreamStates[args.StreamId] = new MyStreamState(args.ClientContext);
};

Audio.AudioInputFrameAsync += async args =>
{
    if (!_myStreamStates.TryGetValue(args.StreamId, out var state)) return;
    state.AddSamples(args.Samples);
    if (args.IsLast) { /* process state.Samples */ }
};
```

### Sound Effect Generation

One-shot — returns a buffered WAV file:

```csharp
var effect = await SoundEffectGenerator.GenerateAsync("Thunder rumbling in the distance");
var wavBytes = await effect.GetDataAsync();  // inline bytes, or downloaded when a large result was delivered as a URL (effect.Kind)
// effect.MimeType, effect.DurationSeconds
```

Use the constructor + config form to stream the effect to clients as it generates, or to set duration/looping:

```csharp
using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
await foreach (var audio in generator.GenerateSoundEffectAsync(new SoundEffectGeneratorConfig
{
    Prompt = "Thunder rumbling in the distance",
    DurationSeconds = 5.0
}))
{
    Audio.SendSpeech(audio);
}
```

---

# Ikon.AI Public API
namespace Ikon.AI.SoundEffectGeneration
  interface ISoundEffectGenerator : IDisposable, ISoundEffectGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    // Streams raw PCM chunks; use GenerateSoundEffectFileAsync for a buffered, encoded audio file instead.
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  sealed class SoundEffectGenerator : ISoundEffectGenerator
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SoundEffectGenerator per call. Returns a buffered WAV file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateSoundEffectFileAsync for duration/looping/prompt-influence, or GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectGeneratorResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectGeneratorResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed record SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    // Applies to the buffered ISoundEffectGenerator.GenerateSoundEffectFileAsync result; the streaming ISoundEffectGenerator.GenerateSoundEffectAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
    TimeSpan Timeout { get; init; }
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(this SoundEffectGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record SoundEffectGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }

namespace Ikon.AI.SpeechGeneration
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  sealed class SpeechGenerator : ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    Task<AudioChunk> GenerateAsync(string text, string? voice = null, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechGenerator per call. Defaults to SpeechGeneratorModel.ElevenFlash25; override via model. Pass voice to pick a voice (model default otherwise). Streamed chunks are concatenated into one PCM AudioChunk. Never returns null — throws RetryableAIException on failure or empty output. Use the constructor + GenerateSpeechAsync for chunk-by-chunk streaming or other fields.
    static Task<AudioChunk> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed record SpeechGeneratorConfig
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    // Speaking-rate multiplier (1.0 = normal); null keeps the model's own default. Honored by OpenAI and Google; ElevenLabs ignores it.
    double? Speed { get; init; }
    string Text { get; init; }
    TimeSpan Timeout { get; init; }
    string VoiceId { get; init; }
  enum SpeechGeneratorModel
    AzureSpeechService
    OpenAITts1
    OpenAITts1Hd
    Gpt4OmniMiniTts
    ElevenFlash2
    ElevenMultilingual2
    ElevenFlash25
    Eleven3
    Eleven3Conversational
    GoogleChirp3
    Gemini25FlashTts
    Gemini25ProTts
    Gemini31FlashTts
  static class SpeechGeneratorModelExtensions
    static string DisplayName(this SpeechGeneratorModel model)

namespace Ikon.AI.SpeechRecognition
  sealed record AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    Task<Transcript> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<TranscriptEvent> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsDiarization { get; }
    bool SupportsPronunciationAnalysis { get; }
    bool SupportsSegmentTimestamps { get; }
    bool SupportsWordTimestamps { get; }
  sealed record Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed record Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  sealed record Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed record Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed record Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  sealed record Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed record Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed record Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed record Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
  sealed record Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
  sealed record Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed record Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
  sealed record RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    // Label each word and segment with a speaker. Throws on a model that cannot diarize.
    bool Diarize { get; init; }
    // Emit revisable interim hypotheses as well as final results. Defaults to false, so only TranscriptEvent.IsFinal events arrive.
    bool InterimResults { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
    // Which timings to ask for; defaults to SpeechTimestamps.None. Only events with TranscriptEvent.IsFinal ever carry words.
    SpeechTimestamps Timestamps { get; init; }
  // Supply the audio exactly one way: raw PCM via Samples or SamplesPcm16 (with SampleRate/ChannelCount), or an encoded audio file via Data (with MimeType), Url, or AssetUri (resolved automatically).
  sealed record RecognizeSpeechConfig
    ctor()
    AssetUri? AssetUri { get; init; }
    int ChannelCount { get; init; }
    byte[]? Data { get; init; }
    // Label each word and segment with a speaker. Throws on a model that cannot diarize.
    bool Diarize { get; init; }
    string Language { get; init; }
    string? MimeType { get; init; }
    string? Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
    // Which timings to ask for; defaults to SpeechTimestamps.None, so an unchanged request costs what it always did. Asking for a granularity the model does not support throws rather than returning empty lists.
    SpeechTimestamps Timestamps { get; init; }
    string? Url { get; init; }
  sealed class SpeechRecognizer : ISpeechRecognizer
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsDiarization { get; }
    bool SupportsPronunciationAnalysis { get; }
    bool SupportsSegmentTimestamps { get; }
    bool SupportsWordTimestamps { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    Task<string> RecognizeAsync(float[] samples, int sampleRate, int channelCount = 1, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a SpeechRecognizer per call. Defaults to SpeechRecognizerModel.WhisperLarge3Turbo; override via model. Returns the recognized text (empty when nothing was recognized). Use the constructor + RecognizeBatchSpeechAsync for a language hint, prompt, or other fields, or RecognizeContinuousSpeechAsync for streaming.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<Transcript> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<TranscriptEvent> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : ISpeechRecognizer
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsDiarization { get; }
    bool SupportsPronunciationAnalysis { get; }
    bool SupportsSegmentTimestamps { get; }
    bool SupportsWordTimestamps { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    Task<Transcript> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<TranscriptEvent> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    // SilenceTriggered mode only: forces recognition after this much continuous speech without a pause. TimeSpan.Zero or negative disables the limit. Defaults to 30s.
    TimeSpan MaxSpeechDuration { get; set; }
    // Defaults to Mode.SilenceTriggered.
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    // Used only in GrowingWindow/SlidingWindow modes (GrowingWindow recognizes all accumulated audio, SlidingWindow only audio since the last run); defaults to 5s.
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    // SilenceTriggered mode only: a pause of this length flushes accumulated speech for recognition. Defaults to 750ms.
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsDiarization { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
    bool SupportsSegmentTimestamps { get; init; }
    bool SupportsWordTimestamps { get; init; }
  enum SpeechRecognizerModel
    AzureSpeechService
    Whisper2
    WhisperLarge3
    WhisperLarge3Turbo
    Gpt4OmniTranscribe
    Gpt4OmniMiniTranscribe
    Gpt4OmniTranscribeDiarize
    GptTranscribe
    DeepgramNova3General
    DeepgramNova3Medical
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversal35Pro
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    ElevenScribe2
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(this SpeechRecognizerModel model)
  // Which timings to ask the provider for. Timestamps cost a larger response and, on some providers, extra processing, so the default is None. Requesting a granularity the model does not support throws — check SpeechRecognizer.GetCapabilities first.
  enum SpeechTimestamps
    None
    Segment
    Word
  // Start and End are relative to the start of the submitted audio, whatever units the provider reported. Speaker is empty unless RecognizeSpeechConfig.Diarize was set and the model supports it.
  sealed record SpeechWord
    ctor()
    double Confidence { get; init; }
    TimeSpan End { get; init; }
    string Speaker { get; init; }
    TimeSpan Start { get; init; }
    string Text { get; init; }
  // The result of one batch transcription. Segments and Words are empty unless RecognizeSpeechConfig.Timestamps asked for them. Language is the language the provider reported detecting, empty when it reported none. Confidence is 0 when the provider does not report one — it is not a score of zero, and no provider that reports confidence reports exactly 0 for real speech.
  sealed record Transcript
    ctor()
    double Confidence { get; init; }
    TimeSpan Duration { get; init; }
    string Language { get; init; }
    IReadOnlyList<TranscriptSegment> Segments { get; init; }
    string Text { get; init; }
    IReadOnlyList<SpeechWord> Words { get; init; }
  // One result from a continuous recognition. IsFinal separates a provider's revisable interim hypothesis from text it will not change: only final events carry Words, because no provider attaches word timings to an interim result. Start and End are relative to the start of the audio stream, so they keep growing for the life of the recognition.
  sealed record TranscriptEvent
    ctor()
    double Confidence { get; init; }
    TimeSpan End { get; init; }
    bool IsFinal { get; init; }
    string Language { get; init; }
    string Speaker { get; init; }
    TimeSpan Start { get; init; }
    string Text { get; init; }
    IReadOnlyList<SpeechWord> Words { get; init; }
  // One provider-chosen span of speech — a Whisper segment, a Deepgram utterance, a diarized speaker turn. Start and End are relative to the start of the submitted audio. Speaker is empty unless diarization was requested and supported.
  sealed record TranscriptSegment
    ctor()
    double Confidence { get; init; }
    TimeSpan End { get; init; }
    string Speaker { get; init; }
    TimeSpan Start { get; init; }
    string Text { get; init; }
