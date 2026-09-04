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
