namespace Ikon.Resonance
  // When Enabled, an AudioMetricsReport is published to Reports once per UpdateIntervalSeconds while packets are being recorded.
  class AudioMetrics
    ctor()
    bool Enabled { get; set; }
    bool LogMetrics { get; set; }
    double UpdateIntervalSeconds { get; set; }
    // A no-op unless Enabled is set to true first — while disabled, nothing is tracked and Reports never yields, so a caller expecting reports must enable the collector before recording.
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    // A single-consumer diagnostics stream: only the latest unread report is kept, and concurrent enumerations compete for reports.
    // cancellationToken: Ends the stream when cancelled.
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
  // Supports mono and stereo audio only; sample rate conversion uses linear interpolation.
  static class AudioResampler
    // inputFrameCount: The number of input frames (samples per channel).
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The desired output sample rate in Hz.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Stereo to mono averages both channels; mono to stereo duplicates the channel.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for converted samples.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // channelCount: The number of channels to check.
    static bool IsSupportedChannelCount(int channelCount)
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for resampled samples.
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The output sample rate in Hz.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static int Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    const int MaxSupportedChannelCount = 2
  static class AudioUtils
    // Output bytes are little-endian; input is clamped to [-1, 1] first. output must be at least 2 * input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for raw bytes. Must be at least twice the length of input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Output bytes are little-endian; input is clamped to [-1, 1] first.
    // input: The input buffer containing float samples.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Input is clamped to [-1, 1] first. output must be at least input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for 16-bit PCM samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Input is clamped to [-1, 1] first.
    // input: The input buffer containing float samples.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Normalizes to [-1, 1]. output must be at least input.Length; throws ArgumentException otherwise. Returns the sample count.
    // input: The input buffer containing 16-bit PCM samples.
    // output: The output buffer for float samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Output is normalized to [-1, 1].
    // input: The input buffer containing 16-bit PCM samples.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Bytes are little-endian; input length must be a multiple of 2 and output at least input.Length / 2. Normalizes to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // output: The output buffer for float samples. Must be at least half the length of input.
    // throws ArgumentException: Thrown when the input length is not a multiple of 2 or output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Output is normalized to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // throws ArgumentException: Thrown when the input length is not a multiple of 2.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // For input normalized to [-1, 1] the result is in [0, 1]. Returns 0 for an empty span; channel layout does not matter.
    // samples: The samples to measure. Channel layout is irrelevant; all samples contribute equally.
    static float Rms(ReadOnlySpan<float> samples)
  // Decides when to interrupt the agent's speech (barge-in): the caller must produce sustained speech for a few consecutive frames, and only after a short grace period from when the agent started speaking, so the first syllables and any echo don't false-trigger.
  sealed class BargeInDetector
    ctor(int sustainedFrames = 3, double graceMs = 300.0)
    void Reset()
    bool ShouldInterrupt(bool isSpeech, bool agentSpeaking, double msSinceSpeakStart)
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
  // Each participant receives a personalized mix of all input streams except those tagged with their own id; every input stream is tagged with its owning participant id (typically a client session id) to control the exclusion. Participants must be registered with AddParticipant before they receive mixed output, streams are added/removed independently via AddStream/RemoveStream, and a participant with no streams of their own still receives output. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    void AddParticipant(int participantId)
    // Re-adding a stream id that is already registered keeps its buffered audio; if the owning participantId differs (the id was reclaimed by a reconnecting participant) the ownership tag is updated so exclusion routing follows the new owner.
    void AddStream(string streamId, int participantId)
    ValueTask DisposeAsync()
    void RemoveParticipant(int participantId)
    // Discards any samples still buffered for the stream. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // The personalized mixes as a stream of 20 ms frames, paced at best-effort real time. Each tick yields one GroupAudioFrame per registered participant, except a participant whose tick mix would contain only their own audio (e.g. a lone speaker), who is skipped for that tick. Single consumer: a concurrent second enumeration throws, but the stream may be re-entered after an enumeration ends (including via an exception unwinding the consumer's loop) — this is how a pump recovers after a frame-handling failure. Yielded frames alias one reused sample buffer — consume (or copy) each frame's samples within the loop body. Cancelling cancellationToken or disposing the mixer ends the stream gracefully: each participant that received audio gets one final empty frame marked PcmAudioFrame.IsLast, then the enumeration completes without throwing.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<GroupAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    // throws ArgumentException: channelCount is less than 1 or sampleRate is not positive.
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Immutable — the mixer captures these values at construction; build a new config (and mixer) to change them.
  sealed record GroupAudioMixerConfig
    ctor()
    double MaxBufferSizeMs { get; init; }
  // The middle of the three audio currencies: AudioChunk is producer audio flowing into a mixer (TTS output, synthesized samples), identified by its speech-event id; PcmAudioFrame is the paced PCM output flowing out of the mixers toward the Opus encoder, identified by its output stream id; the encoded result travels on the wire as the protocol type AudioFrame.
  readonly struct PcmAudioFrame
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
    IReadOnlyList<AudioAnalysisResult>? AnalysisResults { get; }
    int ChannelCount { get; }
    AudioEncoderOptions? EncoderOptions { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    // Interleaved float PCM samples. When a frame comes from SpeechMixer.StreamAsync or GroupAudioMixer.StreamAsync this memory ALIASES a buffer the mixer reuses for the next frame, so it is valid only within the current loop iteration. To keep a frame past the loop body (queue it, hand it to another task), take a self-owned copy first with ToOwned.
    ReadOnlyMemory<float> Samples { get; }
    IReadOnlyList<AudioShapeSetDeclaration>? ShapeSetDeclarations { get; }
    string StreamId { get; }
    IReadOnlyList<int>? TargetIds { get; }
    TimeSpan TotalDuration { get; }
    // Returns a copy whose Samples are backed by a freshly allocated array rather than the mixer's reused buffer, so the copy stays valid after the enumeration advances. Use this whenever a frame from a mixer's StreamAsync must outlive the loop body — storing it, queueing it, or handing it to another task. Every other field is a value, an immutable string, or an already-owned list and is forwarded unchanged.
    PcmAudioFrame ToOwned()
  // Uses asymmetric EMA level tracking, an adaptive noise floor, and a circular pre-buffer so speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Push-based usage: call ProcessChunk per audio chunk and forward non-null results. Stream-based usage: wrap an IAsyncEnumerable<T> source with FilterAsync.
  sealed class SilenceRemover
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, sensible defaults tuned for voice-over-IP audio are used.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional silence remover configuration.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Returns the samples to forward — on speech onset the pre-buffered look-back audio is concatenated in front of the current chunk — or null when the chunk is silence that should be suppressed.
    // chunk: The audio samples to process. Expected to be interleaved float samples in [-1, 1].
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    void Reset()
  // The speech threshold is computed as noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset. Immutable — the remover captures the values at construction; build a new config (and remover) to change them.
  sealed record SilenceRemoverConfig
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; init; }
    float InitialNoiseFloor { get; init; }
    float MaxNoiseFloor { get; init; }
    // How fast the noise floor adapts — during silence only — in (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; init; }
    float NoiseFloorMultiplier { get; init; }
    float NoiseFloorOffset { get; init; }
    int PreBufferMs { get; init; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; init; }
    int SpeechOnsetChunks { get; init; }
    int TrailingSilenceMs { get; init; }
  // Handles one speech event at a time, mixing it into precisely timed 20 ms output frames with smooth fade/crossfade transitions between events.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    AudioEncoderOptions? EncoderOptions { get; set; }
    // Whether output is currently paused (a pending Pause fade-out counts once it completes).
    bool IsPaused { get; }
    string StreamId { get; }
    // The chunk id identifies the speech event: a chunk carrying the current event's id appends to it, while a new id interrupts the current event with the configured fade. Effects, analyzers, and target ids are captured from the event's first chunk; audio is resampled to 48 kHz stereo when needed.
    // throws ArgumentException: The chunk's ChannelCount is less than 1 or its SampleRate is not positive — an object-initialized AudioChunk leaves these at 0; use the full constructor.
    void AddSamples(AudioChunk chunk, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Immediately discards all speech state — current, pending, and paused — without fading. Use for hard resets (e.g. conversation restart); prefer FadeOut for a graceful stop.
    void Clear()
    ValueTask DisposeAsync()
    // Starts fading out the current speech event over the configured fade-out duration. The event completes when the fade reaches silence. No-op when nothing is playing or a fade-out is already in progress.
    void FadeOut()
    // The duration of audio currently buffered for the given speech event, or zero when the event is unknown. Producers that generate faster than real time can use this to pace themselves and keep the bounded mixer buffer from overflowing.
    // speechEventId: The speech event id (the chunk id of the utterance)
    TimeSpan GetBufferedDuration(string speechEventId)
    // Pauses output by fading the current speech out, then holding it (buffered samples are kept) until Resume. No-op when already paused or pausing.
    void Pause()
    // Resumes paused output, fading the held speech event back in from where it stopped. No-op when not paused.
    void Resume()
    // Single consumer: a concurrent second enumeration throws, but the stream may be re-entered after an enumeration ends. Yielded frames alias one reused buffer — consume (or copy) each frame's samples within the loop body. Cancelling cancellationToken or disposing the mixer ends the stream gracefully, emitting a final PcmAudioFrame.IsLast frame when a speech event had started.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<PcmAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Returns a task that completes when the given speech event has finished playing out — its samples fully mixed into the output (pause time included), it was interrupted by a newer event, or it was discarded. Register before or after feeding the event's chunks; an already-completed event resolves immediately. The task also completes when the mixer is cleared or disposed, so callers never hang on a torn-down mixer.
    // speechEventId: The speech event id (the chunk id of the utterance)
    Task WaitForCompletionAsync(string speechEventId)
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
    // Caps effect tail padding in case an effect's output never decays below PaddingThreshold.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60 dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
