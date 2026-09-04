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
    // sampleRate: Mixer output sample rate.
    // channelCount: Mixer output channel count.
    IAudioEffectInstance Create(int sampleRate, int channelCount)
  interface IAudioEffectInstance
    // buffer: The audio buffer to transform.
    void Process(Span<float> buffer)
    void Reset()
  // The parameterless constructor yields a natural small-room reverb (four delay lines, 120–320 ms). For the array constructor, the feedbacks/mixes/delayTimesMs/cutoffFrequencies arrays must all be the same length (one entry per delay line): delay time sets perceived room size, feedback (< 1.0) sets tail length, mix the wet blend, and cutoff damps highs inside the feedback loop.
  sealed class ReverbAudioEffect : IAudioEffect
    ctor()
    // roomSize: Room size from 0 (tiny) to 1 (cathedral). Scales delay times.
    // decay: Reverb tail decay from 0 (short) to 1 (long). Scales feedback.
    // damping: High-frequency damping from 0 (bright) to 1 (dark/muffled).
    // mix: Wet/dry mix from 0 (dry) to 1 (fully wet).
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
