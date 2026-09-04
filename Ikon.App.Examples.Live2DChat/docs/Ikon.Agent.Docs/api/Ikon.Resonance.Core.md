namespace Ikon.Resonance.Core
  // One chunk of a speech event's audio: interleaved float samples plus the format and first/last markers, identified by the speech event's Id. The settable properties and parameterless constructor exist only because the Teleport-generated serializer requires that shape — treat instances as immutable after construction.
  class AudioChunk
    // For the Teleport serializer only. Application code should use the parameterized constructor: an object initializer that skips SampleRate/ChannelCount leaves them at 0, which the mixer rejects with an ArgumentException.
    ctor()
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
  // Codec-agnostic audio encoding options. Immutable — the encoder captures the values when a stream's encoder is created, so a new instance takes effect only for streams whose encoders are created afterwards.
  sealed class AudioEncoderOptions
    ctor(int? bitrate = null, bool? useVBR = null, int? complexity = null)
    // Target bitrate in bits per second (e.g. 128000 for 128 kbps).
    int? Bitrate { get; }
    // Higher = better quality, more CPU. Interpretation is codec-specific (e.g. 0-10 for Opus).
    int? Complexity { get; }
    bool? UseVBR { get; }
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  static class MuLawCodec
    // Output samples are normalized to [-1, 1].
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    // Input samples are clamped to [-1, 1] before encoding.
    static byte[] Encode(ReadOnlySpan<float> samples)
