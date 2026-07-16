# Ikon.Resonance Public API

namespace Ikon.Resonance
  class AudioMetrics
    ctor()
    bool Enabled { get; set; }
    bool LogMetrics { get; set; }
    double UpdateIntervalSeconds { get; set; }
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    IAsyncEnumerable<AudioMetricsReport> Reports(CancellationToken cancellationToken = default)
    void Reset(string streamId)
    void ResetAll()
  sealed record AudioMetricsReport
    ctor(int StreamCount, double MinIpdMs, double AvgIpdMs, double MaxIpdMs, double JitterMs, double AvgEncodeTimeMs, double CpuUsagePercent)
    double AvgEncodeTimeMs { get; init; }
    double AvgIpdMs { get; init; }
    double CpuUsagePercent { get; init; }
    double JitterMs { get; init; }
    double MaxIpdMs { get; init; }
    double MinIpdMs { get; init; }
    int StreamCount { get; init; }
  static class AudioResampler
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    static bool IsSupportedChannelCount(int channelCount)
    static void Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    const int MaxSupportedChannelCount = 2
  static class AudioUtils
    // Output bytes are little-endian; input is clamped to [-1, 1] first. output must be at least 2 * input.Length; throws ArgumentException otherwise.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Input is clamped to [-1, 1] first. output must be at least input.Length; throws ArgumentException otherwise.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Normalizes to [-1, 1]. output must be at least input.Length; throws ArgumentException otherwise. Returns the sample count.
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Bytes are little-endian; input length must be a multiple of 2 and output at least input.Length / 2. Normalizes to [-1, 1].
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // For input normalized to [-1, 1] the result is in [0, 1]. Returns 0 for an empty span; channel layout does not matter.
    static float Rms(ReadOnlySpan<float> samples)
  enum CrossfadeCurve
    Linear
    EqualPower
  enum FadeMode
    Sequential
    Crossfade
  readonly struct GroupAudioFrame
    ctor(int participantId, PcmAudioFrame frame)
    PcmAudioFrame Frame { get; }
    int ParticipantId { get; }
    void Deconstruct(out int participantId, out PcmAudioFrame frame)
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    void AddParticipant(int participantId)
    void AddStream(string streamId, int participantId)
    ValueTask DisposeAsync()
    void RemoveParticipant(int participantId)
    void RemoveStream(string streamId)
    IAsyncEnumerable<GroupAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  sealed record GroupAudioMixerConfig
    ctor()
    double MaxBufferSizeMs { get; init; }
  readonly struct PcmAudioFrame
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
    IReadOnlyList<AudioAnalysisResult>? AnalysisResults { get; }
    int ChannelCount { get; }
    AudioEncoderOptions? EncoderOptions { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    IReadOnlyList<AudioShapeSetDeclaration>? ShapeSetDeclarations { get; }
    string StreamId { get; }
    IReadOnlyList<int>? TargetIds { get; }
    TimeSpan TotalDuration { get; }
  sealed class SilenceRemover
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    void Reset()
  sealed record SilenceRemoverConfig
    ctor()
    float AttackAlpha { get; init; }
    float InitialNoiseFloor { get; init; }
    float MaxNoiseFloor { get; init; }
    float NoiseFloorAlpha { get; init; }
    float NoiseFloorMultiplier { get; init; }
    float NoiseFloorOffset { get; init; }
    int PreBufferMs { get; init; }
    float ReleaseAlpha { get; init; }
    int SpeechOnsetChunks { get; init; }
    int TrailingSilenceMs { get; init; }
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    AudioEncoderOptions? EncoderOptions { get; set; }
    bool IsPaused { get; }
    string StreamId { get; }
    // The chunk id identifies the speech event: a chunk carrying the current event's id appends to it, while a new id interrupts the current event with the configured fade. Effects, analyzers, and target ids are captured from the event's first chunk; audio is resampled to 48 kHz stereo when needed.
    void AddSamples(AudioChunk chunk, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void Clear()
    ValueTask DisposeAsync()
    void FadeOut()
    void Pause()
    void Resume()
    // Enumerable only once per mixer; a second enumeration throws. Yielded frames alias one reused buffer — consume (or copy) each frame's samples within the loop body. Cancelling cancellationToken or disposing the mixer ends the stream gracefully, emitting a final PcmAudioFrame.IsLast frame when a speech event had started.
    IAsyncEnumerable<PcmAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
  // Immutable — the mixer captures these values at construction; build a new config (and mixer) to change them.
  sealed record SpeechMixerConfig
    ctor()
    CrossfadeCurve CrossfadeCurve { get; init; }
    double EndPaddingMs { get; init; }
    double FadeInMs { get; init; }
    FadeMode FadeMode { get; init; }
    double FadeOutMs { get; init; }
    // Upper bound only; the queue grows on demand from a small size. Samples added beyond this bound are dropped with a throttled warning, never thrown.
    double MaxBufferSizeMs { get; init; }
    double MaxPaddingTimeMs { get; init; }
    double PaddingThreshold { get; init; }
  sealed class TurnDetector
    ctor(int sampleRate, int channelCount, TurnDetectorConfig? config = null)
    static IAsyncEnumerable<TurnEvent> DetectAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, TurnDetectorConfig? config = null, CancellationToken ct = default)
    TurnEvent? Flush()
    TurnEvent? Process(ReadOnlyMemory<float> samples)
    void Reset()
  sealed record TurnDetectorConfig
    ctor()
    SilenceRemoverConfig? GateConfig { get; init; }
    TimeSpan MaxTurnDuration { get; init; }
    TimeSpan MinSpeechDuration { get; init; }
    TimeSpan? SpeculativeSilence { get; init; }
    Func<ReadOnlyMemory<float>, bool>? SpeechClassifier { get; init; }
    TimeSpan TurnEndSilence { get; init; }
  readonly struct TurnEvent
    TimeSpan Duration { get; }
    TurnEventKind Kind { get; }
    float[] Samples { get; }
  enum TurnEventKind
    SpeechStarted
    SpeculativeTurnEnd
    SpeechResumed
    TurnEnded
  class WavFile : IDisposable
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    void AddSamples(ReadOnlySpan<short> samples)
    void AddSamples(ReadOnlySpan<float> samples)
    byte[] AsArray()
    Stream AsStream()
    void Dispose()
    void SaveToFile(string filePath)
  enum WavFile.SampleFormat
    Short
    Float

namespace Ikon.Resonance.Analysis
  readonly struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    uint SetId { get; }
    IReadOnlyList<float> Values { get; }
  readonly struct AudioShapeSetDeclaration
    ctor(uint setId, string name, IReadOnlyList<string> shapeNames)
    string Name { get; }
    uint SetId { get; }
    IReadOnlyList<string> ShapeNames { get; }
  interface IAudioAnalyzer
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  interface IAudioAnalyzerInstance
    AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    void Reset()
  sealed class VisemeAnalyzer : IAudioAnalyzer
    ctor()
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)

namespace Ikon.Resonance.Effects
  sealed class BitCrusherAudioEffect : IAudioEffect
    ctor()
    ctor(int bitDepth, int downsampleFactor, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class ChorusAudioEffect : IAudioEffect
    ctor()
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffect
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffectInstance
    void Process(Span<float> buffer)
    void Reset()
  // The parameterless constructor yields a natural small-room reverb (four delay lines, 120–320 ms). For the array constructor, the feedbacks/mixes/delayTimesMs/cutoffFrequencies arrays must all be the same length (one entry per delay line): delay time sets perceived room size, feedback (< 1.0) sets tail length, mix the wet blend, and cutoff damps highs inside the feedback loop.
  sealed class ReverbAudioEffect : IAudioEffect
    ctor()
    ctor(float roomSize, float decay, float damping, float mix)
    ctor(IReadOnlyList<float> feedbacks, IReadOnlyList<float> mixes, IReadOnlyList<float> delayTimesMs, IReadOnlyList<float> cutoffFrequencies)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class RobotVoiceAudioEffect : IAudioEffect
    ctor()
    ctor(float carrierFrequencyHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class SaturationAudioEffect : IAudioEffect
    ctor()
    ctor(float drive, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class TelephoneAudioEffect : IAudioEffect
    ctor()
    ctor(float lowCutHz, float highCutHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  sealed class TremoloAudioEffect : IAudioEffect
    ctor()
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
