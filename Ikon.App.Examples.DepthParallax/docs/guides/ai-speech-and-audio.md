# AI Speech & Audio

## AI Speech & Audio

Text-to-speech with `new SpeechGenerator(model)`, speech-to-text with `new SpeechRecognizer(model)`, and sound effects with `new SoundEffectGenerator(model)`. Audio playback via `Audio.SendSpeech()`.

`Audio` is an app service initialized in your app class: `private Audio Audio { get; } = new(app);`

### Speech Generation (TTS)

```csharp
// Generate speech and stream to clients
using var speechGenerator = new SpeechGenerator(SpeechGeneratorModel.ElevenFlash25);
await foreach (var audio in speechGenerator.GenerateSpeechAsync(new SpeechGeneratorConfig { Text = "Hello world" }))
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

If you need direct access to PCM samples (e.g., custom DSP, your own VAD), subscribe to the audio events and **register per-stream state inside `AudioInputStreamBeginAsync` using `args.ClientSessionId`** — that fires reliably before any frame handler observes a frame from the stream.

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
    abstract IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
  interface ISoundEffectGeneratorInfo
    bool SupportsLooping { get; }
  sealed class SoundEffectFileResult
    byte[] AudioData { get; init; }
    string ContentType { get; init; }
    double DurationSeconds { get; init; }
  sealed class SoundEffectGenerator : IDisposable, ISoundEffectGenerator, ISoundEffectGeneratorInfo
    ctor(string modelName)
    ctor(SoundEffectGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SoundEffectGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsLooping { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateSoundEffectAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    Task<SoundEffectFileResult> GenerateSoundEffectFileAsync(SoundEffectGeneratorConfig config, CancellationToken cancellationToken = null)
    static SoundEffectGeneratorCapabilities GetCapabilities(SoundEffectGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SoundEffectGeneratorModel model)
  sealed class SoundEffectGeneratorCapabilities : ISoundEffectGeneratorInfo
    ctor()
    bool SupportsLooping { get; init; }
  sealed class SoundEffectGeneratorConfig
    ctor()
    double? DurationSeconds { get; set; }
    bool Loop { get; set; }
    string Prompt { get; set; }
    double PromptInfluence { get; set; }
    TimeSpan Timeout { get; set; }
    static SoundEffectGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SoundEffectGeneratorModel
    ElevenLabsV2
  static class SoundEffectGeneratorModelExtensions
    static string DisplayName(SoundEffectGeneratorModel model)

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
    abstract IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
  sealed class SpeechGenerator : IDisposable, ISpeechGenerator
    ctor(string modelName)
    ctor(SpeechGeneratorModel model)
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions)
    ctor(SpeechGeneratorModel model, IReadOnlyList<ModelRegion>? regions)
    int ChannelCount { get; }
    int SampleRate { get; }
    IReadOnlyList<string> VoiceIds { get; }
    void Dispose()
    IAsyncEnumerable<AudioContainer> GenerateSpeechAsync(SpeechGeneratorConfig config, CancellationToken cancellationToken = null)
    static SpeechGeneratorCapabilities GetCapabilities(SpeechGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechGeneratorModel model)
    static IReadOnlyDictionary<SpeechGeneratorModel, IReadOnlyList<string>> GetVoiceIdsByModel()
  sealed class SpeechGeneratorCapabilities
    ctor()
  sealed class SpeechGeneratorConfig
    ctor()
    string Instructions { get; set; }
    string Language { get; set; }
    string Speed { get; set; }
    string Text { get; set; }
    TimeSpan Timeout { get; set; }
    string VoiceId { get; set; }
    static SpeechGeneratorConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class SpeechGeneratorExtensions
    static Task StreamSpeechAsync(ISpeechGenerator speechGenerator, SpeechGeneratorConfig config, Func<AudioContainer, Task> onAudio, CancellationToken cancellationToken = null)
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
    static string DisplayName(SpeechGeneratorModel model)
  static class TextFilter
    static string Filter(string text, TextFilter.Config config)

namespace Ikon.AI.SpeechRecognition
  sealed class AnalyzePronunciationConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string ReferenceText { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    TimeSpan Timeout { get; set; }
    static AnalyzePronunciationConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Break
    ctor()
    int BreakLength { get; init; }
    List<string> ErrorTypes { get; init; }
    Pronunciation.MissingBreak MissingBreak { get; init; }
    Pronunciation.UnexpectedBreak UnexpectedBreak { get; init; }
    static Pronunciation.Break ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizerAdapter.Config
    ctor()
    TimeSpan MaxSpeechDuration { get; set; }
    SpeechRecognizerAdapter.Mode Mode { get; set; }
    TimeSpan RecognitionInterval { get; set; }
    TimeSpan RequestTimeout { get; set; }
    TimeSpan SilenceDuration { get; set; }
    float SilenceThreshold { get; set; }
  sealed class Pronunciation.Feedback
    ctor()
    Pronunciation.Prosody Prosody { get; init; }
    static Pronunciation.Feedback ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  interface ISpeechRecognizer : IDisposable, ISpeechRecognizerInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    abstract Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    abstract Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    abstract IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  interface ISpeechRecognizerInfo
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
  sealed class Pronunciation.Intonation
    ctor()
    List<string> ErrorTypes { get; init; }
    Pronunciation.Monotone Monotone { get; init; }
    static Pronunciation.Intonation ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.MissingBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.MissingBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  enum SpeechRecognizerAdapter.Mode
    GrowingWindow
    SlidingWindow
    SilenceTriggered
  sealed class Pronunciation.Monotone
    ctor()
    double SyllablePitchDeltaConfidence { get; init; }
    static Pronunciation.Monotone ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.NBest
    ctor()
    double Confidence { get; init; }
    string Display { get; init; }
    string ITN { get; init; }
    string Lexical { get; init; }
    string MaskedITN { get; init; }
    Pronunciation.PronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Word> Words { get; init; }
    static Pronunciation.NBest ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Phoneme
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    Pronunciation.PhonemePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Phoneme ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.PhonemePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.PhonemePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  static class Pronunciation
  sealed class Pronunciation.PronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    double CompletenessScore { get; init; }
    double FluencyScore { get; init; }
    double PronScore { get; init; }
    double ProsodyScore { get; init; }
    static Pronunciation.PronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Prosody
    ctor()
    Pronunciation.Break Break { get; init; }
    Pronunciation.Intonation Intonation { get; init; }
    static Pronunciation.Prosody ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeContinuousSpeechConfig
    ctor()
    string[] CandidateLanguages { get; set; }
    int ChannelCount { get; set; }
    string Language { get; set; }
    int SampleRate { get; set; }
    static RecognizeContinuousSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class RecognizeSpeechConfig
    ctor()
    int ChannelCount { get; set; }
    string Language { get; set; }
    string Prompt { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    byte[] SamplesPcm16 { get; set; }
    double Temperature { get; set; }
    TimeSpan Timeout { get; set; }
    static RecognizeSpeechConfig ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Result
    ctor()
    int Channel { get; init; }
    string DisplayText { get; init; }
    long Duration { get; init; }
    string Id { get; init; }
    List<Pronunciation.NBest> NBest { get; init; }
    long Offset { get; init; }
    string RecognitionStatus { get; init; }
    double SNR { get; init; }
    static Pronunciation.Result ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class SpeechRecognizer : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(SpeechRecognizerModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    static SpeechRecognizerCapabilities GetCapabilities(SpeechRecognizerModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(SpeechRecognizerModel model)
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerAdapter : IDisposable, ISpeechRecognizer, ISpeechRecognizerInfo
    ctor(ISpeechRecognizer speechRecognizer, SpeechRecognizerAdapter.Config? config = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsBatchRecognition { get; }
    bool SupportsContinuousRecognition { get; }
    bool SupportsPronunciationAnalysis { get; }
    Task<Pronunciation.Result> AnalyzePronunciationAsync(AnalyzePronunciationConfig config, CancellationToken cancellationToken = null)
    void Dispose()
    Task<string> RecognizeBatchSpeechAsync(RecognizeSpeechConfig config, CancellationToken cancellationToken = null)
    IAsyncEnumerable<string> RecognizeContinuousSpeechAsync(RecognizeContinuousSpeechConfig config, IAsyncEnumerable<float[]> samples, CancellationToken cancellationToken = null)
  sealed class SpeechRecognizerCapabilities : ISpeechRecognizerInfo
    ctor()
    bool SupportsBatchRecognition { get; init; }
    bool SupportsContinuousRecognition { get; init; }
    bool SupportsPronunciationAnalysis { get; init; }
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
    static string DisplayName(SpeechRecognizerModel model)
  sealed class Pronunciation.Syllable
    ctor()
    long Duration { get; init; }
    string Grapheme { get; init; }
    long Offset { get; init; }
    Pronunciation.SyllablePronunciationAssessment PronunciationAssessment { get; init; }
    string Text { get; init; }
    static Pronunciation.Syllable ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.SyllablePronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    static Pronunciation.SyllablePronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.UnexpectedBreak
    ctor()
    double Confidence { get; init; }
    static Pronunciation.UnexpectedBreak ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.Word
    ctor()
    long Duration { get; init; }
    long Offset { get; init; }
    List<Pronunciation.Phoneme> Phonemes { get; init; }
    Pronunciation.WordPronunciationAssessment PronunciationAssessment { get; init; }
    List<Pronunciation.Syllable> Syllables { get; init; }
    string Text { get; init; }
    static Pronunciation.Word ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  sealed class Pronunciation.WordPronunciationAssessment
    ctor()
    double AccuracyScore { get; init; }
    string ErrorType { get; init; }
    Pronunciation.Feedback Feedback { get; init; }
    static Pronunciation.WordPronunciationAssessment ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
