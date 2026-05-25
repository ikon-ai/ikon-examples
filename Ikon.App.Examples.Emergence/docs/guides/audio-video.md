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
  // Extended audio frame with encoding options, analysis results, and target information.
  struct AudioFrameEx
    // Extended audio frame with encoding options, analysis results, and target information.
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId, TimeSpan totalDuration = null, AudioEncoderOptions? encoderOptions = null, IReadOnlyList<int>? targetIds = null, IReadOnlyList<AudioAnalysisResult>? analysisResults = null, IReadOnlyList<AudioShapeSetDeclaration>? shapeSetDeclarations = null)
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
  // Manages multiple audio sources and generates audio frames at a fixed rate (20ms at 48kHz stereo). Supports adding/removing sources dynamically, applying audio effects, and simulating network conditions. All options, sources, and effects can be changed while the generator is running without restart.
  sealed class AudioGenerator
    ctor()
    // Gets a value indicating whether the audio generator is currently running.
    bool IsRunning { get; }
    // Gets the current options. To modify options, use UpdateOptions .
    AudioGeneratorOptions Options { get; }
    // Adds an audio effect to the effects chain. Effects are applied in order to all audio output.
    void AddEffect(IAudioEffect effect)
    // Adds an audio source to the generator.
    string AddSource(IAudioSource source)
    // Removes all audio effects from the effects chain.
    void ClearEffects()
    T GetSource<T>(string streamId) where T : class, IAudioSource
    IEnumerable<ValueTuple<string, T>> GetSourcesOfType<T>() where T : class, IAudioSource
    // Removes an audio effect at the specified index from the effects chain.
    void RemoveEffectAt(int index)
    // Marks an audio source for removal. The source will be removed after its final frame is sent.
    bool RemoveSource(string streamId)
    // Replaces an audio effect at the specified index with a new effect.
    void ReplaceEffect(int index, IAudioEffect newEffect)
    // Starts the audio generation loop asynchronously.
    Task StartAsync(Func<AudioGeneratorFrame, ValueTask> onFrame, Func<string, ValueTask>? onStreamEnd = null, CancellationToken cancellationToken = null)
    // Stops the audio generation loop and waits for it to complete.
    Task StopAsync()
    // Updates the generator options dynamically. Changes take effect on the next frame.
    void UpdateOptions(Action<AudioGeneratorOptions> configure)
  // Output frame from the AudioGenerator.
  struct AudioGeneratorFrame
    // Output frame from the AudioGenerator.
    ctor(ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string streamId)
    int ChannelCount { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    int SampleRate { get; }
    ReadOnlyMemory<float> Samples { get; }
    string StreamId { get; }
  // Configuration options for the AudioGenerator to simulate various network conditions such as jitter, drift, burst transmission, and periodic pauses. All options can be changed dynamically while the generator is running.
  sealed class AudioGeneratorOptions
    ctor()
    // Number of packets to send in each burst.
    int BurstPacketCount { get; set; }
    // Drift factor: 1.0 = realtime, 1.1 = 10% faster, 0.9 = 10% slower.
    double DriftFactor { get; set; }
    // Enable burst mode - sends multiple packets at once, then waits. Exercises buffer overflow handling on the receiver.
    bool EnableBurstMode { get; set; }
    // Enable drift simulation - sends audio faster or slower than real-time. Exercises driftCorrection on the receiver.
    bool EnableDrift { get; set; }
    // Enable jitter simulation - adds random timing variation to each packet. Exercises jitterTracking and adaptiveBuffering on the receiver.
    bool EnableJitter { get; set; }
    // Enable periodic pauses in packet sending. Exercises buffer underrun handling on the receiver.
    bool EnablePause { get; set; }
    // Maximum jitter magnitude in milliseconds. Actual jitter varies from -JitterMs to +JitterMs.
    int JitterMs { get; set; }
    // Duration of each pause in milliseconds.
    int PauseDurationMs { get; set; }
    // Interval between pauses in milliseconds (time of active sending before each pause).
    int PauseIntervalMs { get; set; }
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
    static int MaxSupportedChannelCount
  // High-performance adaptive timer for audio frame pacing. Learns the actual sleep behavior of the OS and adjusts dynamically to minimize CPU usage while maintaining precise timing for audio frame delivery.
  sealed class AudioTimer
    ctor()
    // Resets the timer state. Call when timing context changes significantly (e.g., after pausing/resuming audio, changing audio sources).
    void Reset()
    // Synchronous version for scenarios where async is not available. Uses Thread.Sleep instead of Task.Delay.
    void WaitUntil(long targetTicks, CancellationToken token)
    // Waits until the target time, using adaptive sleeping to minimize CPU usage.
    Task WaitUntilAsync(long targetTicks, CancellationToken token)
  // Provides utility methods for converting audio samples between PCM 16-bit integer and 32-bit float formats.
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
    void AddStream(string streamId, string excludeKey)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(string excludeKey)
    void RemoveStream(string streamId)
    Task StartAsync(Func<string, AudioFrameEx, ValueTask> onFrame, CancellationToken cancellationToken = null)
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer .
  sealed class GroupAudioMixerConfig
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; set; }
  // Represents a source that generates audio frames.
  interface IAudioSource
    // Generates a frame of audio into the provided buffer.
    abstract void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  enum WavFile.SampleFormat
    Short
    Float
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable`1 source with FilterAsync .
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = null)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover . The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset .
  sealed class SilenceRemoverConfig
    ctor()
    // EMA smoothing factor for rising audio levels (0..1). Higher values respond faster to speech onset.
    float AttackAlpha { get; set; }
    // Starting noise floor estimate before any audio has been analyzed.
    float InitialNoiseFloor { get; set; }
    // Upper bound for the adaptive noise floor. Prevents the speech threshold from rising too high in very noisy environments.
    float MaxNoiseFloor { get; set; }
    // How fast the noise floor adapts during silence (0..1). Keep low to prevent speech from contaminating the noise floor estimate.
    float NoiseFloorAlpha { get; set; }
    // Speech threshold multiplier above the noise floor. Higher values are less sensitive and produce fewer false triggers from background noise.
    float NoiseFloorMultiplier { get; set; }
    // Absolute offset added to the speech threshold to prevent it from reaching zero in digital silence. Ensures a minimum sensitivity level.
    float NoiseFloorOffset { get; set; }
    // Milliseconds of recent audio kept in the circular look-back buffer. This audio is emitted on speech onset to preserve word beginnings that would otherwise be clipped.
    int PreBufferMs { get; set; }
    // EMA smoothing factor for falling audio levels (0..1). Lower values decay slower, holding through natural pauses in speech.
    float ReleaseAlpha { get; set; }
    // Number of consecutive above-threshold chunks required to confirm speech onset. Filters transient clicks and noise bursts from triggering false speech detection.
    int SpeechOnsetChunks { get; set; }
    // Milliseconds of trailing audio to include after the last speech chunk. Allows natural word endings and brief pauses to pass through before returning to silence state.
    int TrailingSilenceMs { get; set; }
  // Simplified audio mixer for speech output with precise 20ms frame timing. Handles one speech event at a time with smooth crossfade transitions.
  sealed class SpeechMixer : IAsyncDisposable
    ctor(SpeechMixerConfig? config = null)
    // Encoder options to use for audio output.
    AudioEncoderOptions? EncoderOptions { get; set; }
    bool IsPaused { get; }
    string StreamId { get; }
    void AddSamples(AudioContainer container, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void AddSamples(string speechEventId, ReadOnlySpan<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, IReadOnlyList<int>? targetIds = null)
    void Clear()
    ValueTask DisposeAsync()
    void FadeOut()
    void Pause()
    void Resume()
    Task StartAsync(Func<AudioFrameEx, ValueTask> onFrame, CancellationToken cancellationToken = null)
  // Configuration options for the SpeechMixer.
  sealed class SpeechMixerConfig
    ctor()
    // Crossfade curve type. EqualPower maintains constant perceived loudness.
    CrossfadeCurve CrossfadeCurve { get; set; }
    // Duration of silence padding after speech and effects end (in milliseconds). This prevents fadeout from triggering at natural speech endings.
    double EndPaddingMs { get; set; }
    // Duration of fade-in when speech starts (in milliseconds).
    double FadeInMs { get; set; }
    // Fade transition mode when new speech interrupts current speech. Sequential: fade out completes before fade in starts. Crossfade: fade out and fade in happen simultaneously.
    FadeMode FadeMode { get; set; }
    // Duration of fade-out when speech ends or is interrupted (in milliseconds).
    double FadeOutMs { get; set; }
    // Maximum buffer size in milliseconds for incoming speech samples. This is an upper bound only; the queue grows from a small initial size on demand. Keep this generous enough to absorb production-faster-than-playback bursts (typical for non-streaming TTS) but tight enough that a runaway producer can't consume excessive memory.
    double MaxBufferSizeMs { get; set; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; set; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; set; }
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
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
    // The shape set ID this result belongs to.
    uint SetId { get; set; }
    // The analysis values for this shape set.
    float[] Values { get; set; }
  // Declaration of a shape set with ID and shape names.
  struct AudioShapeSetDeclaration
    // Human-readable name for the shape set (e.g., "Viseme", "Sentiment").
    string Name { get; set; }
    // Unique identifier for this shape set.
    uint SetId { get; set; }
    // Names of each shape in the set, in order (e.g., ["MouthOpenY", "MouthForm"]).
    string[] ShapeNames { get; set; }
  // Factory interface for creating audio analyzer instances. Analyzers extract data from audio without modifying it.
  interface IAudioAnalyzer
    // Gets the shape set declaration for this analyzer. Called once when setting up the audio stream.
    AudioShapeSetDeclaration ShapeSetDeclaration { get; }
    // Creates a stateful analyzer instance bound to the mixer's output format.
    abstract IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  // Stateful audio analyzer that extracts data from audio buffers without modifying them.
  interface IAudioAnalyzerInstance
    // Analyzes the provided buffer and returns shape set values. The buffer is not modified.
    abstract AudioAnalysisResult Analyze(ReadOnlySpan<float> buffer)
    // Resets the analyzer internal state back to its initial values.
    abstract void Reset()
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
    abstract IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateful audio effect that can mutate audio buffers in place.
  interface IAudioEffectInstance
    // Processes the provided buffer in place.
    abstract void Process(Span<float> buffer)
    // Resets the effect internal state back to its initial values.
    abstract void Reset()
  // Factory for creating reverb effects with configurable delay lines, feedback, mix, and damping.
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

namespace Ikon.Resonance.Synth
  // A synthesized drum machine IAudioSource that generates kick, hi-hat, and melody patterns at a specified BPM. Uses synthesis rather than samples for all drum sounds.
  sealed class DrumMachineSource : IAudioSource
    // A synthesized drum machine IAudioSource that generates kick, hi-hat, and melody patterns at a specified BPM. Uses synthesis rather than samples for all drum sounds.
    ctor(double bpm)
    double Bpm { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
  // A simple IAudioSource that generates stereo sine waves from a pentatonic scale. Features slight stereo detuning for a wider sound.
  sealed class SineWaveSource : IAudioSource
    ctor(int frequencyIndex)
    int FrequencyIndex { get; }
    double FrequencyLeft { get; }
    double FrequencyRight { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)

namespace Ikon.Resonance.Synth.Envelopes
  // Implements an Attack-Decay-Sustain-Release (ADSR) envelope generator for amplitude and filter modulation. Uses exponential curves for natural-sounding transitions between stages.
  sealed class AdsrEnvelope
    ctor()
    double Attack { get; set; }
    double Decay { get; set; }
    bool IsActive { get; }
    double Output { get; }
    double Release { get; set; }
    EnvelopeStage Stage { get; }
    double Sustain { get; set; }
    void Gate(bool gate)
    void NoteOff()
    void NoteOn()
    double Process()
    void Reset()
    void SetSampleRate(double sampleRate)
  // Represents the current stage of an ADSR envelope.
  enum EnvelopeStage
    Idle
    Attack
    Decay
    Sustain
    Release

namespace Ikon.Resonance.Synth.Filters
  // Emulates the classic Moog ladder filter, a 4-pole (24dB/octave) low-pass filter with resonance. Features non-linear saturation for analog-style warmth.
  sealed class MoogLadderFilter
    ctor()
    double Cutoff { get; set; }
    double Drive { get; set; }
    double Resonance { get; set; }
    double Process(double input)
    void Reset()
    void SetSampleRate(double sampleRate)

namespace Ikon.Resonance.Synth.Modulation
  // Low Frequency Oscillator (LFO) for modulating synthesizer parameters such as pitch, filter cutoff, and pulse width. Supports multiple waveform shapes and configurable rate.
  sealed class Lfo
    ctor()
    double Phase { get; }
    double Rate { get; set; }
    LfoWaveform Waveform { get; set; }
    double Process()
    void Reset()
    void SetSampleRate(double sampleRate)
    void Sync()
  // Defines the waveform shapes available for the LFO.
  enum LfoWaveform
    Sine
    Triangle
    Saw
    Square
    SampleAndHold

namespace Ikon.Resonance.Synth.Moog
  // A polyphonic virtual analog synthesizer inspired by classic Moog synthesizers. Features dual oscillators, sub-oscillator, Moog ladder filter, dual envelopes, and LFO modulation.
  sealed class MoogSynth
    ctor(int voiceCount = 8)
    Lfo Lfo { get; }
    double NoiseFloor { get; set; }
    MoogSynthPatch Patch { get; set; }
    VoiceAllocator VoiceAllocator { get; }
    void AllNotesOff()
    void ApplyPatch()
    void NoteOff(int noteNumber)
    void NoteOn(int noteNumber, double velocity = 1)
    double Process()
    void Process(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Defines all configurable parameters for the Moog synthesizer including oscillator levels, filter settings, envelope times, LFO modulation, and master volume.
  sealed class MoogSynthPatch
    ctor()
    double AmpAttack { get; set; }
    double AmpDecay { get; set; }
    double AmpRelease { get; set; }
    double AmpSustain { get; set; }
    double DriftAmount { get; set; }
    double FilterAttack { get; set; }
    double FilterCutoff { get; set; }
    double FilterDecay { get; set; }
    double FilterEnvAmount { get; set; }
    double FilterKeyTrack { get; set; }
    double FilterRelease { get; set; }
    double FilterResonance { get; set; }
    double FilterSustain { get; set; }
    double LfoRate { get; set; }
    double LfoToFilter { get; set; }
    double LfoToPitch { get; set; }
    double LfoToPwm { get; set; }
    LfoWaveform LfoWaveform { get; set; }
    double MasterVolume { get; set; }
    string Name { get; set; }
    double NoiseLevel { get; set; }
    double Osc1Level { get; set; }
    double Osc2Level { get; set; }
    double Osc2PulseWidth { get; set; }
    double SubLevel { get; set; }
  // Provides a collection of preset patches for the Moog synthesizer including basses, leads, pads, and brass sounds.
  static class MoogSynthPresets
    static MoogSynthPatch AcidLead()
    static MoogSynthPatch[] All()
    static MoogSynthPatch Brass()
    static MoogSynthPatch FatBass()
    static MoogSynthPatch FilterSweep()
    static MoogSynthPatch LushPad()
    static MoogSynthPatch Pluck()
  // An IAudioSource implementation that wraps the Moog synthesizer and sequencer for use with the audio generator system.
  sealed class MoogSynthSource : IAudioSource
    ctor(MoogSynthPatch? patch = null)
    Sequencer Sequencer { get; }
    MoogSynth Synth { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void NextPattern()
    void SetPatch(MoogSynthPatch patch)
    void SetSequencerMode(SequencerMode mode)

namespace Ikon.Resonance.Synth.Oscillators
  // Defines the interface for audio oscillators that generate periodic waveforms.
  interface IOscillator
    double Phase { get; }
    abstract double Process(double frequency, double sampleRate)
    abstract void Reset()
    abstract void Sync()
  // Defines the available oscillator waveform types.
  enum OscillatorType
    Saw
    Square
    Triangle
    Pulse
    Sine
  // Provides PolyBLEP (Polynomial Band-Limited Step) anti-aliasing for oscillator discontinuities. Reduces aliasing artifacts in sawtooth and square waveforms.
  static class PolyBlep
    static double Compute(double t, double dt)
  // Generates a pulse wave with variable pulse width, using PolyBLEP anti-aliasing. Pulse width can be modulated for PWM (Pulse Width Modulation) effects.
  sealed class PulseOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate, double pulseWidth)
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a sawtooth waveform using PolyBLEP anti-aliasing to reduce aliasing artifacts.
  sealed class SawOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a square wave with variable pulse width, using PolyBLEP anti-aliasing.
  sealed class SquareOscillator : IOscillator
    ctor()
    double Phase { get; }
    double PulseWidth { get; set; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a sub-oscillator square wave one or two octaves below the main oscillator frequency. Adds bass depth and weight to the synthesizer sound.
  sealed class SubOscillator : IOscillator
    ctor()
    int OctaveDown { get; set; }
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()
  // Generates a triangle waveform. Naturally band-limited due to its smooth shape.
  sealed class TriangleOscillator : IOscillator
    ctor()
    double Phase { get; }
    double Process(double frequency, double sampleRate)
    void Reset()
    void Sync()

namespace Ikon.Resonance.Synth.Sequencer
  // Configuration settings for the generative sequencer mode, controlling scale, probability, and velocity parameters.
  sealed class GenerativeSettings
    ctor()
    double Bpm { get; set; }
    double ChordProbability { get; set; }
    double MaxVelocity { get; set; }
    double MinVelocity { get; set; }
    double NoteProbability { get; set; }
    int OctaveRange { get; set; }
    double RestProbability { get; set; }
    int RootNote { get; set; }
    int[] Scale { get; set; }
  // Controls note playback timing for the synthesizer, supporting both pattern-based and generative sequencing modes.
  sealed class Sequencer
    ctor(MoogSynth synth)
    double Bpm { get; }
    GenerativeSettings GenerativeSettings { get; set; }
    SequencerMode Mode { get; set; }
    SequencerPattern Pattern { get; set; }
    void NextPattern()
    void Process(int sampleCount)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Defines the operating mode of the sequencer.
  enum SequencerMode
    Pattern
    Generative
  // Represents a single note in a sequencer pattern with timing and expression data.
  struct SequencerNote
    // Represents a single note in a sequencer pattern with timing and expression data.
    ctor(int noteNumber, double velocity, double duration)
    double Duration { get; }
    int NoteNumber { get; }
    double Velocity { get; }
  // Defines a step-based sequencer pattern with preset patterns for various musical styles.
  sealed class SequencerPattern
    ctor()
    double Bpm { get; set; }
    string Name { get; set; }
    List<SequencerNote?> Steps { get; set; }
    int StepsPerBeat { get; set; }
    static SequencerPattern AcidBass()
    static SequencerPattern Arpeggio()
    static SequencerPattern FilterSweep()
    static SequencerPattern Pad()

namespace Ikon.Resonance.Synth.Songs
  // Represents a complete song with multiple tracks, tempo, and loop length configuration.
  sealed class Song
    ctor()
    double Bpm { get; set; }
    int LoopLengthBeats { get; set; }
    string Name { get; set; }
    List<SongTrack> Tracks { get; set; }
  // Provides a collection of pre-composed demo songs in various synth styles including C64-inspired covers and original compositions.
  static class SongLibrary
    static Song[] All()
    static Song BinaryHorizon()
    static Song CyberChase()
    static Song DigitalDreams()
    static Song LostPatrol()
    static Song NeonPatrol()
    static Song Parallax()
    static Song ShadowRunner()
  // Represents a single note in a song with timing, velocity, and duration information.
  struct SongNote
    // Represents a single note in a song with timing, velocity, and duration information.
    ctor(int noteNumber, double velocity, double duration, double startBeat)
    double Duration { get; }
    int NoteNumber { get; }
    double StartBeat { get; }
    double Velocity { get; }
  // Plays back multi-track songs using multiple Moog synthesizers, handling note timing, looping, and mixing.
  sealed class SongPlayer
    ctor()
    double BeatPosition { get; }
    string CurrentSongName { get; }
    bool IsPlaying { get; }
    Song Song { get; set; }
    void Play()
    void Process(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void Reset()
    void SetSampleRate(double sampleRate)
    void Stop()
  // An IAudioSource implementation that wraps the song player for use with the audio generator system. Supports song switching and playback control.
  sealed class SongPlayerSource : IAudioSource
    ctor(Song? song = null)
    string CurrentSongName { get; }
    SongPlayer Player { get; }
    void GenerateAudio(Span<float> buffer, int samplesPerChannel, int channelCount, int sampleRate)
    void NextSong()
    void Play()
    void Reset()
    void SetSong(Song song)
    void Stop()
  // Represents a track within a song, containing a synthesizer patch and a sequence of notes.
  sealed class SongTrack
    ctor()
    string Name { get; set; }
    List<SongNote> Notes { get; set; }
    MoogSynthPatch Patch { get; set; }

namespace Ikon.Resonance.Synth.Voice
  // Represents a single synthesizer voice with dual oscillators, sub-oscillator, noise, filter, and envelopes. Handles note-on/off events and generates audio samples for one polyphonic voice.
  sealed class SynthVoice
    ctor()
    AdsrEnvelope AmpEnvelope { get; }
    double DriftAmount { get; set; }
    double FilterCutoff { get; set; }
    double FilterEnvAmount { get; set; }
    AdsrEnvelope FilterEnvelope { get; }
    double FilterKeyTrack { get; set; }
    double FilterResonance { get; set; }
    bool IsActive { get; }
    double NoiseLevel { get; set; }
    int NoteNumber { get; }
    double Osc1Level { get; set; }
    double Osc2Level { get; set; }
    double Osc2PulseWidth { get; set; }
    double SubLevel { get; set; }
    double Velocity { get; }
    void NoteOff()
    void NoteOn(int noteNumber, double velocity)
    double Process(double lfoFilterMod, double lfoPitchMod, double lfoPwmMod)
    void Reset()
    void SetSampleRate(double sampleRate)
  // Manages polyphonic voice allocation for the synthesizer. Implements voice stealing with LRU (Least Recently Used) policy when all voices are active.
  sealed class VoiceAllocator
    ctor(int voiceCount = 8)
    int VoiceCount { get; }
    IReadOnlyList<SynthVoice> Voices { get; }
    void AllNotesOff()
    void NoteOff(int noteNumber)
    SynthVoice? NoteOn(int noteNumber, double velocity)
    void Reset()
    void SetSampleRate(double sampleRate)


---

# Ikon.Resonance.Core Public API

namespace Ikon.Resonance.Core
  class AudioContainer
    ctor()
    ctor(string id, float[] samples, int sampleRate, int channelCount, bool isFirst, bool isLast)
    int ChannelCount { get; set; }
    string Id { get; set; }
    bool IsFirst { get; set; }
    bool IsLast { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
    static AudioContainer ReadFromTeleport(ReadOnlySpan<byte> data)
    void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
    static uint TeleportVersion
  // Codec-agnostic audio encoding options.
  class AudioEncoderOptions
    ctor()
    // Target bitrate in bits per second (e.g., 128000 for 128 kbps).
    int? Bitrate { get; set; }
    // Encoder complexity/quality level. Higher = better quality, more CPU. Interpretation is codec-specific (e.g., 0-10 for Opus).
    int? Complexity { get; set; }
    // Enable variable bitrate encoding.
    bool? UseVBR { get; set; }
  class OpusEncoder.EncodedAudio
    ctor()
    float AverageVolume { get; }
    ReadOnlyMemory<byte> Data { get; }
    double EncodingTimeMs { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
  // Decodes Opus-encoded audio data into PCM samples. Wraps the Concentus Opus decoder with buffer management and state reset support.
  class OpusDecoder : IDisposable
    // Decodes Opus-encoded audio data into PCM samples. Wraps the Concentus Opus decoder with buffer management and state reset support.
    ctor(int sampleRate, int channelCount)
    // Decodes Opus-encoded audio data into 32-bit float PCM samples.
    ReadOnlyMemory<float> DecodeAsFloat(ReadOnlySpan<byte> data, bool isFirst, bool decodeFec = false)
    // Decodes Opus-encoded audio data into 16-bit integer PCM samples.
    ReadOnlyMemory<short> DecodeAsShort(ReadOnlySpan<byte> data, bool isFirst, bool decodeFec = false)
    // Releases the resources used by the decoder.
    void Dispose()
  // Encodes PCM audio samples into Opus-compressed packets. Handles frame buffering, encoding timing, and stream start/end markers.
  class OpusEncoder : IDisposable
    // Initializes a new Opus encoder with the specified options.
    ctor(OpusEncoderOptions options)
    // Gets the frame duration in milliseconds used for encoding.
    float FrameDurationMs { get; }
    // Frame size as interleaved PCM samples (all channels)
    int FrameSizeInInterleavedSamples { get; }
    // Frame size per-channel as PCM samples (1 channel). This matches the "frame_size" argument expected by the Opus encoder
    int FrameSizePerChannelInSamples { get; }
    // Releases the resources used by the encoder.
    void Dispose()
    // Encodes PCM audio samples into Opus-compressed packets.
    IEnumerable<OpusEncoder.EncodedAudio> Encode(ReadOnlyMemory<float> samples, bool isFirst, bool isLast)
  // Configuration options for Opus audio encoding.
  class OpusEncoderOptions
    ctor()
    // Application mode: Voip, Audio, or RestrictedLowDelay. Default is Audio.
    OpusApplication? Application { get; set; }
    // Target bitrate in bits per second (e.g., 64000 for 64 kbps). For stereo music, consider 128000-256000.
    int? Bitrate { get; set; }
    // Number of audio channels (1 for mono, 2 for stereo).
    int ChannelCount { get; set; }
    // Encoder complexity from 0-10. Higher = better quality, more CPU. Default is 5.
    int? Complexity { get; set; }
    // Duration of each encoded frame in milliseconds.
    float FrameDurationMs { get; set; }
    // Maximum size of the input sample buffer in milliseconds. The underlying queue grows on demand and is normally near-empty (the encoder consumes one frame per Encode call), so this is a safety cap rather than a working size.
    int InputBufferSizeMs { get; set; }
    // Maximum audio bandwidth: Narrowband, Mediumband, Wideband, Superwideband, or Fullband. Default is Fullband.
    OpusBandwidth? MaxBandwidth { get; set; }
    // Sample rate in Hz.
    int SampleRate { get; set; }
    // Signal type hint: Auto, Voice, or Music. Default is Auto.
    OpusSignal? SignalType { get; set; }
    // Enable constrained VBR (limits peak bitrate). Default is false.
    bool? UseConstrainedVBR { get; set; }
    // Enable variable bitrate encoding. Default is true.
    bool? UseVBR { get; set; }
    // Creates OpusEncoderOptions from generic EncoderOptions.
    static OpusEncoderOptions FromAudioEncoderOptions(AudioEncoderOptions options)
