# Audio & Video

## Audio & Video

### Audio

```csharp
private Audio Audio { get; } = new(app);

// Send audio to clients immediately (sends all audio at once)
await Audio.SendAsync(samples, sampleRate, channelCount, isFirst, isLast, streamId);

// Send speech (throttles output to real-time playback speed and crossfades between sources)
Audio.SendSpeech(audioChunk);

// Receive audio input from client microphone. args carry args.ClientContext /
// args.ClientSessionId / args.UserId — use these directly; do NOT plumb state through
// onCaptureStart to identify the client (use args.ClientSessionId in the handler instead).
Audio.AudioInputStreamBeginAsync += async args => { /* args.StreamId, args.SampleRate, args.ClientSessionId */ };
Audio.AudioInputFrameAsync += async args => { /* args.Samples, args.IsFirst, args.IsLast, args.ClientSessionId */ };
Audio.AudioInputStreamEndAsync += async args => { /* cleanup */ };

// For push-to-talk → chat, prefer the higher-level Audio.SpeechRecognizedAsync / PushToTalkButton —
// see "AI Speech & Audio" section.

// Stream info and cleanup
var info = Audio.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, SampleRate, ChannelCount
await Audio.CloseAsync(streamId);
await Audio.CloseAllAsync();
```

### Video

```csharp
private Video Video { get; } = new(app);

// Receive video input from client camera/screen
Video.VideoInputStreamBeginAsync += async args => { /* args.StreamId, args.Codec, args.Width, args.Height */ };
Video.VideoInputFrameAsync += async args => { /* args.Data, args.FrameNumber, args.IsKey */ };
Video.VideoInputStreamEndAsync += async args => { /* cleanup */ };

// Forward/echo video to other clients
await Video.SendAsync(data, frameNumber, isKey, timestampInUs, durationInUs, codec, width, height, framerate, streamId);

// Stream info and cleanup
var info = Video.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, Width, Height, Framerate
await Video.CloseAsync(streamId);
await Video.CloseAllAsync();
```

Use `CaptureButton` in the UI to start audio/video capture from the client. Render the captured stream to other clients with `view.VideoStreamCanvas(streamId: ...)`.

**Critical: do NOT set `TargetIds` on `ClientVideoCaptureOptions` / `ClientAudioCaptureOptions`** unless you explicitly want to bypass the server. `TargetIds` restricts the WebRTC route to the listed session IDs only — when set to the originating session (or any subset that excludes the server), the server-side `Video.VideoInputStreamBeginAsync` / `Audio.AudioInputStreamBeginAsync` handler never fires, so server-side analysis, recording, and broadcasting all silently break. The browser tab still shows the camera light because local capture is independent. Leave `TargetIds` unset (the default) for the normal "client streams to server, server fans out" flow.

### Audio Effects & Mixer

Apply real-time audio effects via `IAudioEffect` from `Ikon.Resonance.Effects`:

```csharp
using Ikon.Resonance.Effects;

// Available effects: BitCrusherAudioEffect, ChorusAudioEffect, DelayAudioEffect,
// ReverbAudioEffect, RobotVoiceAudioEffect, SaturationAudioEffect,
// TelephoneAudioEffect, TremoloAudioEffect

// Add effects to a SpeechMixer
var mixer = new SpeechMixer();
mixer.AddSamples(container, effects: [new ReverbAudioEffect(), new DelayAudioEffect()]);
```

### Synthesis

Build synthesizers with `Ikon.Resonance.Synth`:

```csharp
using Ikon.Resonance.Synth;
using Ikon.Resonance.Synth.Oscillators;
using Ikon.Resonance.Synth.Filters;
using Ikon.Resonance.Synth.Envelopes;

// Oscillators, filters, envelopes, Moog synth, sequencer
// See Ikon.Resonance Public API reference below for full class listings
```

---

# Ikon.Resonance Public API

namespace Ikon.Resonance
  // Audio frame with samples, stream identity, and optional encoding options, analysis results, and target information.
  struct AudioFrame
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
  // Tracks audio stream metrics including packet counts, inter-packet delays, jitter, and encoding times. Supports tracking metrics across multiple streams.
  class AudioMetrics
    ctor()
    double AvgEncodeTimeMs { get; }
    double AvgIpdMs { get; }
    double CpuUsagePercent { get; }
    bool Enabled { get; set; }
    double JitterMs { get; }
    bool LogMetrics { get; set; }
    double MaxIpdMs { get; }
    double MinIpdMs { get; }
    int StreamCount { get; }
    double UpdateIntervalSeconds { get; set; }
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    void Reset(string streamId)
    void ResetAll()
    event Action? Updated
  // Provides methods for resampling audio between different sample rates and channel configurations. Supports mono and stereo audio using linear interpolation for sample rate conversion.
  static class AudioResampler
    // Calculates the number of output frames after resampling.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Converts audio between mono and stereo channel configurations. Stereo to mono averages both channels; mono to stereo duplicates the channel.
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // Determines whether the specified channel count is supported.
    static bool IsSupportedChannelCount(int channelCount)
    // Resamples audio from one sample rate and channel configuration to another using linear interpolation.
    static void Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    // The maximum number of audio channels supported (mono or stereo).
    const int MaxSupportedChannelCount
  // Provides utility methods for measuring audio levels and converting audio samples between PCM 16-bit integer and 32-bit float formats.
  static class AudioUtils
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // Computes the root mean square (RMS) level of the samples. For normalized float audio in [-1.0, 1.0] the result is in [0.0, 1.0] and is the standard measure of perceived loudness (e.g. for silence detection thresholds).
    static float Rms(ReadOnlySpan<float> samples)
  // Crossfade curve type.
  enum CrossfadeCurve
    Linear
    EqualPower
  // Fade transition mode when new speech interrupts current speech.
  enum FadeMode
    Sequential
    Crossfade
  // Server-side audio mixer for group voice scenarios (meetings, conferences, multiplayer). Mixes multiple participant audio streams together, producing a personalized output stream for each participant that contains all other participants' audio mixed together but excludes the participant's own audio. Each input stream is tagged with an excludeKey (typically a participant/session ID) to control the exclusion. Participants must be registered with AddParticipant before they can receive mixed output. Streams are added/removed independently via AddStream and RemoveStream . A participant continues to receive output (from other participants' streams) even when they have no active streams of their own. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    // Registers a participant to receive personalized mixed audio output. The participant will receive a mix of all streams except those tagged with their excludeKey.
    void AddParticipant(string excludeKey)
    // Registers an input audio stream and tags it with excludeKey so the owning participant never hears their own audio. Adding an already-registered stream id is a no-op.
    void AddStream(string streamId, string excludeKey)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(string excludeKey)
    // Unregisters an input stream and discards any samples still buffered for it. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // Starts the output loop that paces personalized mixes into onFrame as 20 ms frames (called once per registered participant per tick, with the participant's excludeKey as the first argument — except a participant whose tick mix would contain only their own audio, e.g. a lone speaker, who is skipped for that tick). May be called only once per mixer instance; a second call throws so a silently dropped onFrame can never go unnoticed. Buffer-reuse contract: the frames passed to onFrame alias a single reused sample buffer — consume the samples fully before returning from the callback and copy them if you need to store them beyond the call.
    Task StartAsync(Func<string, AudioFrame, ValueTask> onFrame, CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer . Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed class GroupAudioMixerConfig : IEquatable<GroupAudioMixerConfig>
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; init; }
  // Represents a source that generates audio frames.
  interface IAudioSource
    // Generates a frame of audio into the provided buffer.
    abstract void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  // Specifies the sample format used in the WAV file.
  enum WavFile.SampleFormat
    Short
    Float
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable source with FilterAsync .
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover . The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset . Immutable — the remover captures the values at construction, so construct a new config (and remover) instead of mutating a shared instance.
  sealed class SilenceRemoverConfig : IEquatable<SilenceRemoverConfig>
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; init; }
    // Starting noise floor estimate before any audio has been analyzed.
    float InitialNoiseFloor { get; init; }
    // Upper bound for the adaptive noise floor. Prevents the speech threshold from rising too high in very noisy environments.
    float MaxNoiseFloor { get; init; }
    // How fast the noise floor adapts during silence (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; init; }
    // Speech threshold multiplier above the noise floor. Higher values are less sensitive and produce fewer false triggers from background noise.
    float NoiseFloorMultiplier { get; init; }
    // Absolute offset added to the speech threshold to prevent it from reaching zero in digital silence. Ensures a minimum sensitivity level.
    float NoiseFloorOffset { get; init; }
    // Milliseconds of recent audio kept in the circular look-back buffer. This audio is emitted on speech onset to preserve word beginnings that would otherwise be clipped.
    int PreBufferMs { get; init; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; init; }
    // Number of consecutive above-threshold chunks required to confirm speech onset. Filters transient clicks and noise bursts from triggering false speech detection.
    int SpeechOnsetChunks { get; init; }
    // Milliseconds of trailing audio to include after the last speech chunk. Allows natural word endings and brief pauses to pass through before returning to silence state.
    int TrailingSilenceMs { get; init; }
  // Simplified audio mixer for speech output with precise 20ms frame timing. Handles one speech event at a time with smooth crossfade transitions.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    // Encoder options to use for audio output.
    AudioEncoderOptions? EncoderOptions { get; set; }
    // Whether output is currently paused (a pending Pause fade-out counts once it completes).
    bool IsPaused { get; }
    // Stable identifier stamped on every output frame this mixer emits.
    string StreamId { get; }
    // Feeds a chunk of speech audio into the mixer, resampling to 48 kHz stereo when needed. The chunk's id identifies the speech event: chunks with the current event's id append to it, while a new id interrupts the current event with the configured fade transition. Effects, analyzers, and target ids are captured from the event's first chunk.
    void AddSamples(AudioChunk chunk, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    // Immediately discards all speech state — current, pending, and paused — without fading. Use for hard resets (e.g. conversation restart); prefer FadeOut for a graceful stop.
    void Clear()
    ValueTask DisposeAsync()
    // Starts fading out the current speech event over the configured fade-out duration. The event completes when the fade reaches silence. No-op when nothing is playing or a fade-out is already in progress.
    void FadeOut()
    // Pauses output by fading the current speech out, then holding it (buffered samples are kept) until Resume . No-op when already paused or pausing.
    void Pause()
    // Resumes paused output, fading the held speech event back in from where it stopped. No-op when not paused.
    void Resume()
    // Starts the output loop that paces mixed audio into onFrame as 20 ms frames. May be called only once per mixer instance; a second call throws so a silently dropped onFrame can never go unnoticed. Buffer-reuse contract: the frames passed to onFrame alias a single reused sample buffer — consume the samples fully before returning from the callback and copy them if you need to store them beyond the call.
    Task StartAsync(Func<AudioFrame, ValueTask> onFrame, CancellationToken cancellationToken = default)
  // Configuration options for the SpeechMixer. Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed class SpeechMixerConfig : IEquatable<SpeechMixerConfig>
    ctor()
    // Crossfade curve type. EqualPower maintains constant perceived loudness.
    CrossfadeCurve CrossfadeCurve { get; init; }
    // Duration of silence padding after speech and effects end (in milliseconds). This prevents fadeout from triggering at natural speech endings.
    double EndPaddingMs { get; init; }
    // Duration of fade-in when speech starts (in milliseconds).
    double FadeInMs { get; init; }
    // Fade transition mode when new speech interrupts current speech. Sequential: fade out completes before fade in starts. Crossfade: fade out and fade in happen simultaneously.
    FadeMode FadeMode { get; init; }
    // Duration of fade-out when speech ends or is interrupted (in milliseconds).
    double FadeOutMs { get; init; }
    // Maximum buffer size in milliseconds for incoming speech samples. This is an upper bound only; the queue grows from a small initial size on demand. Keep this generous enough to absorb production-faster-than-playback bursts (typical for non-streaming TTS) but tight enough that a runaway producer can't consume excessive memory. Samples added beyond this bound are dropped (with a throttled warning) rather than throwing; the backing buffer is released once the event drains, so this only caps the transient in-flight footprint.
    double MaxBufferSizeMs { get; init; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // Detects conversational turns in a continuous (open-mic) audio stream: speech onset, probable turn end (speculative), speech resumption, and confirmed turn end — the segmentation an always-listening voice app needs between "raw mic frames" and "transcribe and respond". Deterministic: time is counted in received samples, not wall-clock, so the same frame sequence always produces the same events. This assumes the source keeps delivering frames during silence (true for platform mic capture, which streams continuously while active). Usage — push-based: call TurnDetector.Process per audio chunk and act on the returned event. Usage — stream-based: wrap an IAsyncEnumerable<T> source with TurnDetector.DetectAsync.
  sealed class TurnDetector
    // Creates a new TurnDetector for the given audio format.
    ctor(int sampleRate, int channelCount, TurnDetectorConfig? config = null)
    // Wraps an async audio source, yielding turn events as they occur. When the source completes, a still-open turn is flushed as a final TurnEventKind.TurnEnded event.
    static IAsyncEnumerable<TurnEvent> DetectAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, TurnDetectorConfig? config = null, CancellationToken ct = default)
    // Reports the end of the audio stream. A confirmed turn still in progress is finalized and returned as a TurnEventKind.TurnEnded event; otherwise returns null. The detector is reset either way.
    TurnEvent? Flush()
    // Processes one audio chunk (interleaved float samples in [-1, 1]) and returns the transition it caused, or null when nothing changed.
    TurnEvent? Process(ReadOnlyMemory<float> samples)
    // Resets all internal state to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for TurnDetector. Immutable — construct a new config (and detector) instead of mutating a shared instance.
  sealed class TurnDetectorConfig : IEquatable<TurnDetectorConfig>
    ctor()
    // Tuning for the built-in level gate and the onset pre-buffer (SilenceRemoverConfig.PreBufferMs). Only the level-tracking and pre-buffer fields apply; the onset/trailing fields belong to SilenceRemover. When null, SilenceRemover defaults are used except SilenceRemoverConfig.ReleaseAlpha is raised to 0.3 — turn detection needs the level to fall promptly when speech stops (the hold-through-pauses role is played by TurnDetectorConfig.TurnEndSilence instead), where the slow default would add noticeable latency to every turn end.
    SilenceRemoverConfig? GateConfig { get; init; }
    // Maximum turn length; a turn still running at this point is force-ended.
    TimeSpan MaxTurnDuration { get; init; }
    // Minimum cumulative speech required before a turn is confirmed. Shorter bursts (coughs, clicks) are discarded without producing any events.
    TimeSpan MinSpeechDuration { get; init; }
    // Silence duration after which the turn has probably ended and a TurnEventKind.SpeculativeTurnEnd fires, letting downstream work start before the turn end is certain. Must be shorter than TurnDetectorConfig.TurnEndSilence. Null disables speculative turn ends.
    TimeSpan? SpeculativeSilence { get; init; }
    // Optional external speech classifier (e.g. a neural VAD such as Silero) that replaces the built-in adaptive level gate. Receives one chunk of interleaved float PCM and returns whether it contains speech. Null uses the built-in gate.
    Func<ReadOnlyMemory<float>, bool>? SpeechClassifier { get; init; }
    // Silence duration that ends a turn. This window — not the level gate — provides the "hold through natural pauses" behavior, so mid-sentence breaths don't split a turn.
    TimeSpan TurnEndSilence { get; init; }
  // A transition reported by TurnDetector. TurnEvent.Samples carries the utterance audio for TurnEventKind.SpeculativeTurnEnd and TurnEventKind.TurnEnded (including pre-buffered onset audio) and is empty for the other kinds.
  struct TurnEvent
    // Duration of TurnEvent.Samples; zero when no audio is carried.
    TimeSpan Duration { get; }
    // The kind of transition.
    TurnEventKind Kind { get; }
    // Utterance samples (interleaved float PCM), or empty for events that carry no audio.
    float[] Samples { get; }
  // The kind of transition reported by TurnDetector.
  enum TurnEventKind
    SpeechStarted
    SpeculativeTurnEnd
    SpeechResumed
    TurnEnded
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
    // Initializes a new WAV file builder with the specified audio parameters.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // Adds 16-bit integer audio samples to the WAV file.
    void AddSamples(ReadOnlySpan<short> samples)
    // Adds 32-bit float audio samples to the WAV file.
    void AddSamples(ReadOnlySpan<float> samples)
    // Gets the WAV file as a byte array. Finalizes the WAV header if not already done.
    byte[] AsArray()
    // Gets the WAV file as a readable stream. Finalizes the WAV header if not already done.
    Stream AsStream()
    // Releases the resources used by the WAV file builder.
    void Dispose()
    // Saves the WAV file to disk. Finalizes the WAV header if not already done.
    void SaveToFile(string filePath)

namespace Ikon.Resonance.Analysis
  // Result of audio analysis containing shape set values.
  struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    // The shape set ID this result belongs to.
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  // Declaration of a shape set with ID and shape names.
  struct AudioShapeSetDeclaration
    ctor(uint setId, string name, IReadOnlyList<string> shapeNames)
    // Human-readable name for the shape set (e.g., "Viseme", "Sentiment").
    string Name { get; }
    // Unique identifier for this shape set.
    uint SetId { get; }
    // Names of each shape in the set, in order (e.g., ["MouthOpenY", "MouthForm"]).
    IReadOnlyList<string> ShapeNames { get; }
  // Factory interface for creating audio analyzer instances. Analyzers extract data from audio without modifying it.
  interface IAudioAnalyzer
    // Gets the shape set declaration for this analyzer. Called once when setting up the audio stream.
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    // Creates a stateful analyzer instance bound to the mixer's output format.
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  // Stateful audio analyzer that extracts data from audio buffers without modifying them.
  interface IAudioAnalyzerInstance
    // Analyzes the provided buffer and returns shape set values. The buffer is not modified.
    AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    // Resets the analyzer internal state back to its initial values.
    void Reset()
  // Audio analyzer that performs FFT-based spectral analysis for viseme (lip sync) detection. Produces MouthOpenY (0-1) from RMS and MouthForm (-1 to +1) from spectral analysis.
  sealed class VisemeAnalyzer : IAudioAnalyzer
    ctor()
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)

namespace Ikon.Resonance.Effects
  // Low-fidelity effect that reduces both bit depth and sample rate.
  sealed class BitCrusherAudioEffect : IAudioEffect
    ctor()
    ctor(int bitDepth, int downsampleFactor, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Classic chorus with modulated delay that gently widens mono or stereo sources.
  sealed class ChorusAudioEffect : IAudioEffect
    ctor()
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Feedback delay that adds spacious echoes with gentle high-frequency damping.
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateless definition of an audio effect that can create mixer-ready instances.
  interface IAudioEffect
    // Creates a stateful effect instance bound to the mixer's output format.
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateful audio effect that can mutate audio buffers in place.
  interface IAudioEffectInstance
    // Processes the provided buffer in place.
    void Process(Span<float> buffer)
    // Resets the effect internal state back to its initial values.
    void Reset()
  // Factory for creating reverb effects with configurable delay lines, feedback, mix, and damping.
  // Remarks:
  // If you just want a quick, natural-sounding reverb, call the constructor without parameters. The defaults create four delay lines (120ms – 320ms) with moderate feedback and a low-pass damping around 2kHz, which mimics a small room. To tweak the sound: • Delay times control the perceived room size. Longer times feel larger. • Feedback controls tail length. Values close to 1.0 sustain longer. • Mix defines how much of each delay line is blended into the output. • Cutoff damps high frequencies inside the feedback loop for warmer tails. All arrays must have the same length, so each delay line has a matching set of parameters.
  sealed class ReverbAudioEffect : IAudioEffect
    // Creates a reverb with default room parameters (small room).
    ctor()
    // Creates a reverb with simplified parameters for easy room modeling.
    ctor(float roomSize, float decay, float damping, float mix)
    // Creates a reverb with full control over all delay line parameters.
    ctor(IReadOnlyList<float> feedbacks, IReadOnlyList<float> mixes, IReadOnlyList<float> delayTimesMs, IReadOnlyList<float> cutoffFrequencies)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Metallic robot voice using ring modulation and mild saturation.
  sealed class RobotVoiceAudioEffect : IAudioEffect
    ctor()
    ctor(float carrierFrequencyHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Soft saturation that adds harmonic richness while keeping peaks controlled.
  sealed class SaturationAudioEffect : IAudioEffect
    ctor()
    ctor(float drive, float mix)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Narrowband telephone-style filter with gentle saturation.
  sealed class TelephoneAudioEffect : IAudioEffect
    ctor()
    ctor(float lowCutHz, float highCutHz, float mix, float drive)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Amplitude modulation (tremolo) with optional stereo phase offset for movement.
  sealed class TremoloAudioEffect : IAudioEffect
    ctor()
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90)
    IAudioEffectInstance Create(int sampleRate, int channelCount)


---

# Ikon.Resonance.Core Public API

namespace Ikon.Resonance.Core
  // One chunk of a speech event's audio: interleaved float samples plus the format and first/last markers, identified by the speech event's Id . Mutable with settable properties and a parameterless constructor because the Teleport-generated serializer requires that shape — treat instances as immutable after construction.
  class AudioChunk
    ctor()
    ctor(string id, float[] samples, int sampleRate, int channelCount, bool isFirst, bool isLast)
    int ChannelCount { get; set; }
    string Id { get; set; }
    bool IsFirst { get; set; }
    bool IsLast { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
  // Codec-agnostic audio encoding options. Immutable — the encoder captures the values when a stream's encoder is created, so construct a new instance instead of mutating a shared one.
  sealed class AudioEncoderOptions
    ctor(int? bitrate = null, bool? useVBR = null, int? complexity = null)
    // Target bitrate in bits per second (e.g., 128000 for 128 kbps).
    int? Bitrate { get; }
    // Encoder complexity/quality level. Higher = better quality, more CPU. Interpretation is codec-specific (e.g., 0-10 for Opus).
    int? Complexity { get; }
    // Enable variable bitrate encoding.
    bool? UseVBR { get; }
  // Controls when incoming audio frames are output to listeners
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
