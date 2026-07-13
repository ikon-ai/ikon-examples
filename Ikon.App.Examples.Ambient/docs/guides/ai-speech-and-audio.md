# AI Speech & Audio

## AI Speech & Audio

Text-to-speech with `Audio.SpeakAsync(text)`, speech-to-text with `SpeechRecognizer.RecognizeAsync(samples, sampleRate)`, and sound effects with `SoundEffectGenerator.GenerateAsync(prompt)`. Audio playback via `Audio.SendSpeech()`.

`Audio` is an app service initialized in your app class: `private Audio Audio { get; } = new(app);`

### Speech Generation (TTS)

```csharp
// Generate speech and play it to clients — one call. A new call fades out and
// replaces whatever is still playing (the interrupt behavior a voice app wants).
await Audio.SpeakAsync("Hello world");

// Pick a model/voice, shape the delivery, or target specific clients:
await Audio.SpeakAsync("Hello world", SpeechGeneratorModel.Eleven3, voice: "Aria", targetIds: [clientSessionId]);
await Audio.SpeakAsync("Hello world", instructions: "Whisper, as if sharing a secret", speed: 1.2);  // speed is a double, 1.0 = normal
```

To get the audio WITHOUT playing it (e.g. to store or post-process a clip), use the one-shot `SpeechGenerator.GenerateAsync(text)` — it returns a single PCM `AudioChunk` (never null; throws `SpeechGeneratorException` on failure):

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

If you need direct access to PCM samples (e.g., custom DSP, your own VAD), subscribe to the audio events and **register per-stream state inside `AudioInputStreamBeginAsync` using `args.ClientSessionId`** — that fires reliably before any frame handler observes a frame from the stream. To transcribe samples you already hold, use the one-shot `var text = await SpeechRecognizer.RecognizeAsync(samples, sampleRate);` (WhisperLarge3Turbo, cheap+fast, by default).

```csharp
Audio.AudioInputStreamBeginAsync += async args =>
{
    // Snapshot per-stream state here. args.ClientSessionId / args.UserId identify the client.
    _myStreamStates[args.StreamId] = new MyState(args.ClientContext);
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
// effect.AudioData (WAV bytes), effect.ContentType, effect.DurationSeconds
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
    // Channel count of the PCM samples produced by ISoundEffectGenerator.GenerateSoundEffectAsync.
    int ChannelCount { get; }
    // Sample rate of the PCM samples produced by ISoundEffectGenerator.GenerateSoundEffectAsync.
    int SampleRate { get; }
    // Streams the generated sound effect as PCM AudioChunk chunks as they are produced. Use ISoundEffectGenerator.GenerateSoundEffectFileAsync for a buffered, encoded audio file instead.
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    // Generates the sound effect and returns it as a single buffered, encoded audio file (WAV).
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  class NonRetryableSoundEffectGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SoundEffectFileResult
    ctor()
    required byte[] AudioData { get; init; }
    required string ContentType { get; init; }
    required double DurationSeconds { get; init; }
  sealed class SoundEffectGenerator : IDisposable, ISoundEffectGenerator, ISoundEffectGeneratorInfo
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    // Generate a sound-effect file from a plain prompt — the instance form of the SoundEffectGenerator.GenerateAsync one-shot, for when you already hold a generator. Reach for SoundEffectGenerator.GenerateSoundEffectFileAsync when the request needs any other SoundEffectGeneratorConfig field.
    Task<SoundEffectFileResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // One-shot sound effect generation. The verbose form
    // using var generator = new SoundEffectGenerator(SoundEffectGeneratorModel.ElevenLabsV2);
    // var result = await generator.GenerateSoundEffectFileAsync(new SoundEffectGeneratorConfig { Prompt = prompt });
    // becomes
    // var effect = await SoundEffectGenerator.GenerateAsync(prompt);
    // Defaults to SoundEffectGeneratorModel.ElevenLabsV2 (the only sound effect model). Returns a buffered WAV file (.AudioData / .ContentType / .DurationSeconds). Reach for the constructor + SoundEffectGenerator.GenerateSoundEffectFileAsync when you need a target duration, looping, prompt influence, or any other SoundEffectGeneratorConfig field beyond the prompt; use SoundEffectGenerator.GenerateSoundEffectAsync for streaming PCM chunks.
    static Task<SoundEffectFileResult> GenerateAsync(string prompt, SoundEffectGeneratorModel model = ElevenLabsV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = default)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig : IEquatable<SoundEffectGeneratorConfig>
    ctor()
    double? DurationSeconds { get; init; }
    bool Loop { get; init; }
    string Prompt { get; init; }
    double PromptInfluence { get; init; }
    TimeSpan Timeout { get; init; }
  class SoundEffectGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(this SoundEffectGeneratorModel model)

namespace Ikon.AI.SpeechGeneration
  sealed class TextFilter.Config
    ctor()
    int MaxTextLength { get; set; }
    bool RemoveEmojis { get; set; }
    bool SimplifyUrls { get; set; }
    bool SpeakOnlyFirstParagraph { get; set; }
  interface ISpeechGenerator : IDisposable
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
  class NonRetryableSpeechGeneratorException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    // Speak a line of text and collect it into one audio chunk — the instance form of the SpeechGenerator.GenerateAsync one-shot, for when you already hold a generator. Reach for SpeechGenerator.GenerateSpeechAsync when you want the chunks as they stream, or any other SpeechGeneratorConfig field.
    Task<AudioChunk> GenerateAsync(string text, string? voice = null, CancellationToken cancellationToken = default)
    // One-shot text-to-speech. The verbose form
    // using var generator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
    // await foreach (var chunk in generator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = text }))
    // {
    //     // collect chunk.Samples
    // }
    // becomes
    // var audio = await SpeechGenerator.GenerateAsync(text);
    // Defaults to SpeechGeneratorModel.ElevenFlash25 (cheap+fast). Override the model via the second parameter when the task warrants; pass voice to pick a voice (the model's default voice otherwise). The streamed chunks are concatenated into a single PCM AudioChunk (.Samples / .SampleRate / .ChannelCount). Never returns null — throws a SpeechGeneratorException when generation fails or the model produces no audio, so wrap in try/catch when the app should continue without the audio. Reach for the constructor + SpeechGenerator.GenerateSpeechAsync when you need chunk-by-chunk streaming playback while generation runs, or any other SpeechGeneratorConfig field beyond text+voice (language, instructions, speed).
    static Task<AudioChunk> GenerateAsync(string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = default)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorConfig : IEquatable<SpeechGeneratorConfig>
    ctor()
    string Instructions { get; init; }
    string Language { get; init; }
    // Speaking-rate multiplier as an invariant-culture decimal string — "1.0" is normal speed, "0.75" slower, "1.5" faster. Empty means "leave the model's default". A string rather than a number for wire-compatibility reasons only. Honored per provider: OpenAI passes it through as speed (an unparseable value silently falls back to 1.0 — no error is raised), Google maps it to speakingRate (an unparseable value is silently ignored), ElevenLabs ignores it.
    string Speed { get; init; }
    string Text { get; init; }
    TimeSpan Timeout { get; init; }
    string VoiceId { get; init; }
  class SpeechGeneratorException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SpeechGeneratorModel
    AzureSpeechService
    OpenAITts1
    OpenAITts1Hd
    Gpt4OmniMiniTts
    ElevenFlash2
    ElevenMultilingual2
    ElevenFlash25
    Eleven3
    GoogleChirp3
    Gemini25FlashTts
    Gemini25ProTts
    Gemini31FlashTts
  static class SpeechGeneratorModelExtensions
    static string DisplayName(this SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig : IEquatable<AnalyzePronunciationConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string ReferenceText { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Break : IEquatable<Pronunciation.Break>
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    // The maximum duration of continuous speech before recognition is forced in SilenceTriggered mode. This prevents indefinite buffering when the speaker doesn't pause. Set to TimeSpan.Zero or negative to disable the limit.
    TimeSpan MaxSpeechDuration { get; set; }
    // The recognition mode that determines how audio is segmented and when recognition is triggered.
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    // The interval at which speech recognition is triggered in GrowingWindow and SlidingWindow modes. In GrowingWindow mode, recognition runs on all accumulated audio at this interval. In SlidingWindow mode, recognition runs on the audio collected since the last recognition.
    TimeSpan RecognitionInterval { get; set; }
    // The timeout for individual speech recognition API requests.
    TimeSpan RequestTimeout { get; set; }
    // The duration of continuous silence required to trigger recognition in SilenceTriggered mode. When the speaker pauses for this duration, the accumulated speech is sent for recognition.
    TimeSpan SilenceDuration { get; set; }
    // The amplitude threshold below which audio is considered silence. Sample values with absolute amplitude below this threshold are treated as silent.
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback : IEquatable<Pronunciation.Feedback>
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation : IEquatable<Pronunciation.Intonation>
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
  sealed class Pronunciation.MissingBreak : IEquatable<Pronunciation.MissingBreak>
    ctor()
    double Confidence { get; init; }
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone : IEquatable<Pronunciation.Monotone>
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
  sealed class Pronunciation.NBest : IEquatable<Pronunciation.NBest>
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
  class NonRetryableSpeechRecognizerException : NonRetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  sealed class Pronunciation.Phoneme : IEquatable<Pronunciation.Phoneme>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.PhonemePronunciationAssessment : IEquatable<Pronunciation.PhonemePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment : IEquatable<Pronunciation.PronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
  sealed class Pronunciation.Prosody : IEquatable<Pronunciation.Prosody>
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
  sealed class RecognizeContinuousSpeechConfig : IEquatable<RecognizeContinuousSpeechConfig>
    ctor()
    string[] CandidateLanguages { get; init; }
    int ChannelCount { get; init; }
    string Language { get; init; }
    int SampleRate { get; init; }
  sealed class RecognizeSpeechConfig : IEquatable<RecognizeSpeechConfig>
    ctor()
    int ChannelCount { get; init; }
    string Language { get; init; }
    string Prompt { get; init; }
    int SampleRate { get; init; }
    float[] Samples { get; init; }
    byte[] SamplesPcm16 { get; init; }
    double Temperature { get; init; }
    TimeSpan Timeout { get; init; }
  sealed class Pronunciation.Result : IEquatable<Pronunciation.Result>
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    // Transcribe a buffer of samples — the instance form of the SpeechRecognizer.RecognizeAsync one-shot, for when you already hold a recognizer. Reach for SpeechRecognizer.RecognizeBatchSpeechAsync when the request needs any other RecognizeSpeechConfig field (language, prompt, timestamps).
    Task<string> RecognizeAsync(float[] samples, int sampleRate, int channelCount = 1, CancellationToken cancellationToken = default)
    // One-shot batch transcription. The verbose form
    // using var recognizer = new SpeechRecognizer(SpeechRecognizerModel.WhisperLarge3Turbo);
    // var text = await recognizer.RecognizeBatchSpeechAsync(new RecognizeSpeechConfig
    // {
    //     Samples = samples,
    //     SampleRate = 16000,
    //     ChannelCount = 1
    // });
    // becomes
    // var text = await SpeechRecognizer.RecognizeAsync(samples, 16000);
    // Defaults to SpeechRecognizerModel.WhisperLarge3Turbo (cheap+fast). Override the model via the third parameter when the task warrants. Returns the recognized text (empty when nothing was recognized). Reach for the constructor + SpeechRecognizer.RecognizeBatchSpeechAsync when you need PCM16 byte input, a language hint, a prompt, or any other RecognizeSpeechConfig field; use SpeechRecognizer.RecognizeContinuousSpeechAsync for streaming recognition.
    static Task<string> RecognizeAsync(float[] samples, int sampleRate, SpeechRecognizerModel model = WhisperLarge3Turbo, int channelCount = 1, CancellationToken cancellationToken = default)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = default)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = default)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = default)
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
  class SpeechRecognizerException : RetryableAIException
    ctor()
    ctor(string message)
    ctor(string message, Exception inner)
  enum SpeechRecognizerModel
    AzureSpeechService
    Whisper2
    WhisperLarge3
    WhisperLarge3Turbo
    Gpt4OmniTranscribe
    Gpt4OmniMiniTranscribe
    DeepgramNova3General
    AssemblyAIUniversal3ProStreaming
    AssemblyAIUniversalStreamingEnglish
    AssemblyAIUniversalStreamingMultilingual
    VoxtralMiniTranscribe2
  static class SpeechRecognizerModelExtensions
    static string DisplayName(this SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable : IEquatable<Pronunciation.Syllable>
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.SyllablePronunciationAssessment : IEquatable<Pronunciation.SyllablePronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
  sealed class Pronunciation.UnexpectedBreak : IEquatable<Pronunciation.UnexpectedBreak>
    ctor()
    double Confidence { get; init; }
  sealed class Pronunciation.Word : IEquatable<Pronunciation.Word>
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
  sealed class Pronunciation.WordPronunciationAssessment : IEquatable<Pronunciation.WordPronunciationAssessment>
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
