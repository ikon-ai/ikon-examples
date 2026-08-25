# Audio & Video

## Audio & Video

### Audio

```csharp
private Audio Audio { get; } = new(app);

// Three ways to send audio — pick by how delivery is paced:

// 1. Speech (TTS or AudioChunks through the speech mixer): real-time paced, new speech
//    interrupts current speech with a fade. The default for spoken replies.
await Audio.SpeakAsync(text);
Audio.SendSpeech(audioChunk);

// 2. Complete clip (decoded file, generated music): real-time paced, no mixer interruption.
//    Await completes when the clip has been fully sent (≈ clip duration).
await Audio.StreamAsync(samples, sampleRate, channelCount, streamId, cancellationToken: ct);

// 3. Immediate, UNPACED transmit — only for audio already produced in real time (e.g. echoing
//    mic frames back out) or very short clips. A long clip sent this way arrives all at once
//    and can overflow client audio buffers — use StreamAsync for clips instead.
await Audio.SendImmediateAsync(samples, sampleRate, channelCount, isFirst, isLast, streamId);

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

// Forward/echo video to other clients. Frames are transmitted immediately — call once per
// frame at the source framerate (e.g. forward each incoming frame as it arrives); never loop
// over a stored clip's frames without pacing.
await Video.SendFrameAsync(data, frameNumber, isKey, timestampInUs, durationInUs, codec, width, height, framerate, streamId);

// Stream info and cleanup
var info = Video.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, Width, Height, Framerate
await Video.CloseAsync(streamId);
await Video.CloseAllAsync();
```

Use `CaptureButton` in the UI to start audio/video capture from the client. Render the captured stream to other clients with `view.VideoStreamCanvas(streamId: ...)`.

Captured media always routes to the app on the server — `Video.VideoInputStreamBeginAsync` / `Audio.AudioInputStreamBeginAsync` fire there, and the other clients never receive the raw capture. The app decides any fan-out (e.g. `Audio.SendSpeech` / `Video.SendFrameAsync` with explicit targets).

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
  // Tracks audio stream metrics including packet counts, inter-packet delays, jitter, and encoding times. Supports tracking metrics across multiple streams. When Enabled, an AudioMetricsReport is published to Reports once per UpdateIntervalSeconds while packets are being recorded.
  class AudioMetrics
    ctor()
    bool Enabled { get; set; }
    bool LogMetrics { get; set; }
    double UpdateIntervalSeconds { get; set; }
    // Records one packet for streamId. This is a no-op unless Enabled is set to true first — while disabled, nothing is tracked and Reports never yields, so a caller expecting reports must enable the collector before recording.
    void RecordPacket(string streamId, double encodingTimeMs)
    void Remove(string streamId)
    // The interval snapshots as an async stream. A single-consumer diagnostics stream: only the latest unread report is kept, and concurrent enumerations compete for reports.
    // cancellationToken: Ends the stream when cancelled.
    IAsyncEnumerable<AudioMetricsReport> Reports(CancellationToken cancellationToken = default)
    void Reset(string streamId)
    void ResetAll()
  // One interval snapshot of audio stream metrics published by AudioMetrics.
  sealed record AudioMetricsReport
    ctor(int StreamCount, double MinIpdMs, double AvgIpdMs, double MaxIpdMs, double JitterMs, double AvgEncodeTimeMs, double CpuUsagePercent)
    double AvgEncodeTimeMs { get; init; }
    double AvgIpdMs { get; init; }
    double CpuUsagePercent { get; init; }
    double JitterMs { get; init; }
    double MaxIpdMs { get; init; }
    double MinIpdMs { get; init; }
    int StreamCount { get; init; }
  // Provides methods for resampling audio between different sample rates and channel configurations. Supports mono and stereo audio using linear interpolation for sample rate conversion.
  static class AudioResampler
    // Calculates the number of output frames after resampling.
    // inputFrameCount: The number of input frames (samples per channel).
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The desired output sample rate in Hz.
    static int CalculateResampledFrameCount(int inputFrameCount, int inputSampleRate, int outputSampleRate)
    // Converts audio between mono and stereo channel configurations. Stereo to mono averages both channels; mono to stereo duplicates the channel.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for converted samples.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static void ConvertChannels(ReadOnlySpan<float> source, Span<float> destination, int inputChannelCount, int outputChannelCount)
    // Determines whether the specified channel count is supported.
    // channelCount: The number of channels to check.
    static bool IsSupportedChannelCount(int channelCount)
    // Resamples audio from one sample rate and channel configuration to another using linear interpolation.
    // source: The source audio samples in interleaved format.
    // destination: The destination buffer for resampled samples.
    // inputSampleRate: The input sample rate in Hz.
    // outputSampleRate: The output sample rate in Hz.
    // inputChannelCount: The number of input channels (1 or 2).
    // outputChannelCount: The number of output channels (1 or 2).
    static int Resample(ReadOnlySpan<float> source, Span<float> destination, int inputSampleRate, int outputSampleRate, int inputChannelCount, int outputChannelCount)
    // The maximum number of audio channels supported (mono or stereo).
    const int MaxSupportedChannelCount = 2
  // Provides utility methods for measuring audio levels and converting audio samples between PCM 16-bit integer and 32-bit float formats.
  static class AudioUtils
    // Output bytes are little-endian; input is clamped to [-1, 1] first. output must be at least 2 * input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for raw bytes. Must be at least twice the length of input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input, Span<byte> output)
    // Converts 32-bit float samples to 16-bit PCM samples as raw bytes (little-endian). Float values are clamped to [-1.0, 1.0] before conversion.
    // input: The input buffer containing float samples.
    static byte[] ConvertFloatToPcm16Bytes(ReadOnlySpan<float> input)
    // Input is clamped to [-1, 1] first. output must be at least input.Length; throws ArgumentException otherwise.
    // input: The input buffer containing float samples.
    // output: The output buffer for 16-bit PCM samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input, Span<short> output)
    // Converts 32-bit float samples to 16-bit PCM samples. Float values are clamped to [-1.0, 1.0] before conversion.
    // input: The input buffer containing float samples.
    static short[] ConvertFloatToPcm16Shorts(ReadOnlySpan<float> input)
    // Normalizes to [-1, 1]. output must be at least input.Length; throws ArgumentException otherwise. Returns the sample count.
    // input: The input buffer containing 16-bit PCM samples.
    // output: The output buffer for float samples. Must be at least as long as input.
    // throws ArgumentException: Thrown when the output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<short> input, Span<float> output)
    // Converts 16-bit PCM samples to 32-bit float samples normalized to the range [-1.0, 1.0].
    // input: The input buffer containing 16-bit PCM samples.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<short> input)
    // Bytes are little-endian; input length must be a multiple of 2 and output at least input.Length / 2. Normalizes to [-1, 1].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // output: The output buffer for float samples. Must be at least half the length of input.
    // throws ArgumentException: Thrown when the input length is not a multiple of 2 or output buffer is too small.
    static int ConvertPcm16ToFloat(ReadOnlySpan<byte> input, Span<float> output)
    // Converts 16-bit PCM samples (as raw bytes) to 32-bit float samples normalized to the range [-1.0, 1.0].
    // input: The input buffer containing raw bytes representing 16-bit PCM samples (little-endian).
    // throws ArgumentException: Thrown when the input length is not a multiple of 2.
    static float[] ConvertPcm16ToFloat(ReadOnlySpan<byte> input)
    // For input normalized to [-1, 1] the result is in [0, 1]. Returns 0 for an empty span; channel layout does not matter.
    // samples: The samples to measure. Channel layout is irrelevant; all samples contribute equally.
    static float Rms(ReadOnlySpan<float> samples)
  // Decides when to interrupt the agent's speech (barge-in): the caller must produce sustained speech for a few consecutive frames, and only after a short grace period from when the agent started speaking (so the first syllables / any echo don't false-trigger). Pure logic — unit tested.
  sealed class BargeInDetector
    ctor(int sustainedFrames = 3, double graceMs = 300.0)
    void Reset()
    bool ShouldInterrupt(bool isSpeech, bool agentSpeaking, double msSinceSpeakStart)
  // Crossfade curve type.
  enum CrossfadeCurve
    // Linear crossfade (amplitude-based). Can have a perceived dip in the middle.
    Linear
    // Equal power crossfade (power-based). Maintains constant perceived loudness. Uses sine/cosine curves: fadeOut = cos(t * π/2), fadeIn = sin(t * π/2)
    EqualPower
  // Fade transition mode when new speech interrupts current speech.
  enum FadeMode
    // Fade out completes before fade in starts.
    Sequential
    // Fade out and fade in happen simultaneously.
    Crossfade
  // One personalized output frame from a GroupAudioMixer: the participant it is addressed to plus their mixed audio.
  readonly struct GroupAudioFrame
    ctor(int participantId, PcmAudioFrame frame)
    // The mixed audio frame (all other participants' streams, excluding the participant's own).
    PcmAudioFrame Frame { get; }
    // The participant this mix is addressed to.
    int ParticipantId { get; }
    void Deconstruct(out int participantId, out PcmAudioFrame frame)
  // Server-side audio mixer for group voice scenarios (meetings, conferences, multiplayer). Mixes multiple participant audio streams together, producing a personalized output stream for each participant that contains all other participants' audio mixed together but excludes the participant's own audio. Each input stream is tagged with the id of the participant it belongs to (typically a client session id) to control the exclusion. Participants must be registered with AddParticipant before they can receive mixed output. Streams are added/removed independently via AddStream and RemoveStream. A participant continues to receive output (from other participants' streams) even when they have no active streams of their own. Uses power-preserving normalization (1/sqrt(N)) and tanh soft-clipping to prevent distortion when many participants speak simultaneously.
  sealed class GroupAudioMixer : IAsyncDisposable
    ctor(GroupAudioMixerConfig? config = null)
    // Registers a participant to receive personalized mixed audio output. The participant will receive a mix of all streams except those tagged with their own id.
    void AddParticipant(int participantId)
    // Registers an input audio stream and tags it with the owning participantId so that participant never hears their own audio. Re-adding a stream id that is already registered keeps its buffered audio; if the owning participantId differs (the id was reclaimed by a reconnecting participant) the ownership tag is updated so exclusion routing follows the new owner.
    void AddStream(string streamId, int participantId)
    ValueTask DisposeAsync()
    // Unregisters a participant. They will no longer receive mixed audio output.
    void RemoveParticipant(int participantId)
    // Unregisters an input stream and discards any samples still buffered for it. Removing an unknown stream id is a no-op.
    void RemoveStream(string streamId)
    // The personalized mixes as a stream of 20 ms frames, paced at best-effort real time. Each tick yields one GroupAudioFrame per registered participant — except a participant whose tick mix would contain only their own audio (e.g. a lone speaker), who is skipped for that tick. The caller owns the loop: run await foreach over the stream and forward each frame to its participant. Single consumer: a concurrent second enumeration throws, but once an enumeration ends (including by an exception unwinding the consumer's loop) the stream may be re-entered — this is how a pump recovers after a frame-handling failure. Buffer-reuse contract: the yielded frames alias a single reused sample buffer — consume the samples fully within the loop body and copy them if you need to store them beyond it. Cancelling cancellationToken (or disposing the mixer) ends the stream gracefully: each participant that received audio gets one final empty frame marked PcmAudioFrame.IsLast so downstream consumers can close their streams, then the enumeration completes without throwing.
    // cancellationToken: Ends the stream when cancelled.
    // throws InvalidOperationException: Thrown when the mixer is already streaming.
    IAsyncEnumerable<GroupAudioFrame> StreamAsync(CancellationToken cancellationToken = default)
    // Buffers interleaved samples for a registered input stream, resampling to the mixer's native 48 kHz stereo format when needed. When the stream's buffer is full the oldest samples are dropped to make room; writes to an unknown stream are dropped with a throttled warning (stream teardown races with in-flight frames, so this is not an error).
    // throws ArgumentException: channelCount is less than 1 or sampleRate is not positive.
    void WriteSamples(string streamId, ReadOnlySpan<float> samples, int sampleRate, int channelCount)
  // Configuration for the GroupAudioMixer. Immutable — the mixer captures the values at construction, so construct a new config (and mixer) instead of mutating a shared instance.
  sealed record GroupAudioMixerConfig
    ctor()
    // Maximum buffer size per stream in milliseconds.
    double MaxBufferSizeMs { get; init; }
  // One in-process frame of raw PCM audio: interleaved float samples plus stream identity and optional encoding options, analysis results, and target information. This is the middle of the three audio currencies. AudioChunk is producer audio flowing INTO a mixer (TTS output, synthesized samples), identified by its speech-event id. PcmAudioFrame is the paced PCM output flowing OUT of the mixers toward the Opus encoder, identified by its output stream id. The encoded result travels on the wire as the protocol type AudioFrame.
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
  // Filters silence from an audio chunk stream so that only speech reaches downstream consumers such as speech-to-text models (which tend to hallucinate on silent input). Uses asymmetric EMA for level tracking, an adaptive noise floor, and a circular pre-buffer to ensure speech onsets are never clipped. Designed for real-time audio at typical frame sizes (20 ms). Usage — push-based: call ProcessChunk per audio chunk, forward non-null results. Usage — stream-based: wrap an IAsyncEnumerable<T> source with FilterAsync.
  sealed class SilenceRemover
    // Creates a new SilenceRemover for the given audio format.
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, sensible defaults tuned for voice-over-IP audio are used.
    ctor(int sampleRate, int channelCount, SilenceRemoverConfig? config = null)
    // Wraps an async audio source, yielding only chunks that contain speech. Silence is suppressed and speech onsets include look-back audio from the pre-buffer.
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional silence remover configuration.
    // ct: Cancellation token.
    static IAsyncEnumerable<float[]> FilterAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, SilenceRemoverConfig? config = null, CancellationToken ct = default)
    // Processes a single audio chunk and determines whether it should be forwarded downstream. Returns the samples to forward (including pre-buffered onset audio when speech begins), or null if the chunk is silence that should be suppressed.
    // chunk: The audio samples to process. Expected to be interleaved float samples in [-1, 1].
    float[]? ProcessChunk(ReadOnlySpan<float> chunk)
    // Resets all internal state (EMA level, noise floor, pre-buffer, and state machine) to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for SilenceRemover. The silence remover uses asymmetric EMA (exponential moving average) to track audio level, an adaptive noise floor that adjusts to the environment, and a circular pre-buffer that preserves the onset of speech so words are never clipped. The speech threshold is computed as: noiseFloor * NoiseFloorMultiplier + NoiseFloorOffset. Immutable — the remover captures the values at construction, so construct a new config (and remover) instead of mutating a shared instance.
  sealed record SilenceRemoverConfig
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
    // Upper bound only; the queue grows on demand from a small size. Samples added beyond this bound are dropped with a throttled warning, never thrown.
    double MaxBufferSizeMs { get; init; }
    // Maximum padding duration in milliseconds for effect tails. Prevents infinite padding if effects never fully decay.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // Detects conversational turns in a continuous (open-mic) audio stream: speech onset, probable turn end (speculative), speech resumption, and confirmed turn end — the segmentation an always-listening voice app needs between "raw mic frames" and "transcribe and respond". Deterministic: time is counted in received samples, not wall-clock, so the same frame sequence always produces the same events. This assumes the source keeps delivering frames during silence (true for platform mic capture, which streams continuously while active). Usage — push-based: call Process per audio chunk and act on the returned event. Usage — stream-based: wrap an IAsyncEnumerable<T> source with DetectAsync.
  sealed class TurnDetector
    // Creates a new TurnDetector for the given audio format.
    // sampleRate: Sample rate of the incoming audio in Hz (e.g. 48000).
    // channelCount: Number of audio channels (e.g. 1 for mono).
    // config: Optional configuration. When null, defaults tuned for conversational voice are used.
    ctor(int sampleRate, int channelCount, TurnDetectorConfig? config = null)
    // Wraps an async audio source, yielding turn events as they occur. When the source completes, a still-open turn is flushed as a final TurnEventKind.TurnEnded event.
    // source: The async enumerable producing audio chunks.
    // sampleRate: Sample rate of the audio in Hz.
    // channelCount: Number of audio channels.
    // config: Optional turn detector configuration.
    // ct: Cancellation token.
    static IAsyncEnumerable<TurnEvent> DetectAsync(IAsyncEnumerable<float[]> source, int sampleRate, int channelCount, TurnDetectorConfig? config = null, CancellationToken ct = default)
    // Reports the end of the audio stream. A confirmed turn still in progress is finalized and returned as a TurnEventKind.TurnEnded event; otherwise returns null. The detector is reset either way.
    TurnEvent? Flush()
    // Processes one audio chunk (interleaved float samples in [-1, 1]) and returns the transition it caused, or null when nothing changed.
    TurnEvent? Process(ReadOnlyMemory<float> samples)
    // Resets all internal state to initial values. Call this when starting a new audio session on the same instance.
    void Reset()
  // Configuration for TurnDetector. Immutable — construct a new config (and detector) instead of mutating a shared instance.
  sealed record TurnDetectorConfig
    ctor()
    // Tuning for the built-in level gate and the onset pre-buffer (SilenceRemoverConfig.PreBufferMs). Only the level-tracking and pre-buffer fields apply; the onset/trailing fields belong to SilenceRemover. When null, SilenceRemover defaults are used except SilenceRemoverConfig.ReleaseAlpha is raised to 0.3 — turn detection needs the level to fall promptly when speech stops (the hold-through-pauses role is played by TurnEndSilence instead), where the slow default would add noticeable latency to every turn end.
    SilenceRemoverConfig? GateConfig { get; init; }
    // Maximum turn length; a turn still running at this point is force-ended.
    TimeSpan MaxTurnDuration { get; init; }
    // Minimum cumulative speech required before a turn is confirmed. Shorter bursts (coughs, clicks) are discarded without producing any events.
    TimeSpan MinSpeechDuration { get; init; }
    // Silence duration after which the turn has probably ended and a TurnEventKind.SpeculativeTurnEnd fires, letting downstream work start before the turn end is certain. Must be shorter than TurnEndSilence. Null disables speculative turn ends.
    TimeSpan? SpeculativeSilence { get; init; }
    // Optional external speech classifier (e.g. a neural VAD such as Silero) that replaces the built-in adaptive level gate. Receives one chunk of interleaved float PCM and returns whether it contains speech. Null uses the built-in gate.
    Func<ReadOnlyMemory<float>, bool>? SpeechClassifier { get; init; }
    // Silence duration that ends a turn. This window — not the level gate — provides the "hold through natural pauses" behavior, so mid-sentence breaths don't split a turn.
    TimeSpan TurnEndSilence { get; init; }
  // A transition reported by TurnDetector. Samples carries the utterance audio for TurnEventKind.SpeculativeTurnEnd and TurnEventKind.TurnEnded (including pre-buffered onset audio) and is empty for the other kinds.
  readonly struct TurnEvent
    // Duration of Samples; zero when no audio is carried.
    TimeSpan Duration { get; }
    // The kind of transition.
    TurnEventKind Kind { get; }
    // Utterance samples (interleaved float PCM), or empty for events that carry no audio.
    float[] Samples { get; }
  // The kind of transition reported by TurnDetector.
  enum TurnEventKind
    // The user has produced sustained speech (at least TurnDetectorConfig.MinSpeechDuration).
    SpeechStarted
    // Silence has lasted TurnDetectorConfig.SpeculativeSilence — the turn has probably ended. Carries the utterance audio so far, so downstream work (transcription, a reply) can start early. Followed by either SpeechResumed (the guess was wrong) or TurnEnded.
    SpeculativeTurnEnd
    // Speech resumed after a SpeculativeTurnEnd — discard the speculative result.
    SpeechResumed
    // The turn has ended: silence lasted TurnDetectorConfig.TurnEndSilence (or the turn hit TurnDetectorConfig.MaxTurnDuration). Carries the complete utterance audio.
    TurnEnded
  // Creates WAV audio files in memory with support for 16-bit integer or 32-bit float sample formats. Samples are written incrementally and the WAV header is finalized when the file is accessed.
  class WavFile : IDisposable
    // Initializes a new WAV file builder with the specified audio parameters.
    // sampleRate: The sample rate in Hz (e.g., 44100, 48000).
    // channelCount: The number of audio channels (1 for mono, 2 for stereo).
    // sampleFormat: The sample format to use for the WAV file.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // Adds 16-bit integer audio samples to the WAV file.
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Short.
    void AddSamples(ReadOnlySpan<short> samples)
    // Adds 32-bit float audio samples to the WAV file.
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Float.
    void AddSamples(ReadOnlySpan<float> samples)
    // Gets the WAV file as a byte array. Finalizes the WAV header if not already done.
    byte[] AsArray()
    // Gets the WAV file as a fresh readable stream over a copy of the data. Finalizes the WAV header if not already done. The returned stream is independent of this WavFile, so it survives disposal of the builder and each call returns its own stream.
    Stream AsStream()
    // Releases the resources used by the WAV file builder.
    void Dispose()
    // Saves the WAV file to disk. Finalizes the WAV header if not already done.
    // filePath: The path where the WAV file will be saved.
    void SaveToFile(string filePath)
  // Specifies the sample format used in the WAV file.
  enum WavFile.SampleFormat
    // 16-bit signed integer PCM format.
    Short
    // 32-bit IEEE floating-point format.
    Float

namespace Ikon.Resonance.Analysis
  // Result of audio analysis containing shape set values.
  readonly struct AudioAnalysisResult
    ctor(uint setId, IReadOnlyList<float> values)
    // The shape set ID this result belongs to.
    uint SetId { get; }
    // The analysis values for this shape set. Analyzers may reuse the backing storage between frames — copy the values if you need them beyond the current frame.
    IReadOnlyList<float> Values { get; }
  // Declaration of a shape set with ID and shape names.
  readonly struct AudioShapeSetDeclaration
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
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioAnalyzerInstance Create(int sampleRate, int channelCount)
  // Stateful audio analyzer that extracts data from audio buffers without modifying them.
  interface IAudioAnalyzerInstance
    // Analyzes the provided buffer and returns shape set values. The buffer is not modified.
    // buffer: The audio buffer to analyze (interleaved samples).
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
    ctor(float baseDelayMs, float depthMs, float rateHz, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Feedback delay that adds spacious echoes with gentle high-frequency damping.
  sealed class DelayAudioEffect : IAudioEffect
    ctor()
    ctor(float delayMs, float feedback, float mix, float feedbackDamping = 0.25f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateless definition of an audio effect that can create mixer-ready instances.
  interface IAudioEffect
    // Creates a stateful effect instance bound to the mixer's output format.
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  // Stateful audio effect that can mutate audio buffers in place.
  interface IAudioEffectInstance
    // Processes the provided buffer in place.
    // buffer: The audio buffer to transform.
    void Process(Span<float> buffer)
    // Resets the effect internal state back to its initial values.
    void Reset()
  // The parameterless constructor yields a natural small-room reverb (four delay lines, 120–320 ms). For the array constructor, the feedbacks/mixes/delayTimesMs/cutoffFrequencies arrays must all be the same length (one entry per delay line): delay time sets perceived room size, feedback (< 1.0) sets tail length, mix the wet blend, and cutoff damps highs inside the feedback loop.
  sealed class ReverbAudioEffect : IAudioEffect
    // Creates a reverb with default room parameters (small room).
    ctor()
    // Creates a reverb with simplified parameters for easy room modeling.
    // roomSize: Room size from 0 (tiny) to 1 (cathedral). Scales delay times.
    // decay: Reverb tail decay from 0 (short) to 1 (long). Scales feedback.
    // damping: High-frequency damping from 0 (bright) to 1 (dark/muffled).
    // mix: Wet/dry mix from 0 (dry) to 1 (fully wet).
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
    ctor(float rateHz, float depth, float mix, float stereoPhaseOffsetDegrees = 90f)
    IAudioEffectInstance Create(int sampleRate, int channelCount)


---

# Ikon.Resonance.Core Public API

namespace Ikon.Resonance.Core
  // One chunk of a speech event's audio: interleaved float samples plus the format and first/last markers, identified by the speech event's Id. Mutable with settable properties and a parameterless constructor because the Teleport-generated serializer requires that shape — treat instances as immutable after construction.
  class AudioChunk
    // For the Teleport serializer only. Application code should use the parameterized constructor: an object initializer that skips SampleRate/ChannelCount leaves them at 0, which the mixer rejects with an ArgumentException.
    ctor()
    // Builds a chunk with all required fields — the recommended construction path.
    // id: The speech-event id. One unique id per utterance; a multi-chunk stream shares the id across its chunks. Reusing a completed utterance's id silently drops the chunk, and an older id starts a new utterance that interrupts what is playing.
    // samples: Interleaved float PCM samples in [-1, 1].
    // sampleRate: Samples per second; must be positive.
    // channelCount: Interleaved channel count; must be at least 1.
    // isFirst: True for the first chunk of the utterance.
    // isLast: True for the last chunk of the utterance.
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
    // Output frames immediately as they arrive
    Streaming
    // Buffer frames until the total duration is known
    DelayUntilTotalDurationKnown
    // Buffer all frames until the last frame arrives (full buffering)
    DelayUntilIsLast
  // G.711 mu-law codec for telephony audio (8-bit, 8 kHz), the encoding carried on the wire by every telephony media stream we speak to — Twilio Media Streams and 46elks Realtime Voice both offer it natively. Converts between mu-law bytes and normalized float samples.
  static class MuLawCodec
    // Decodes mu-law bytes to float samples normalized to [-1.0, 1.0].
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    // Encodes float samples (normalized to [-1.0, 1.0]) to mu-law bytes.
    static byte[] Encode(ReadOnlySpan<float> samples)
