# Ikon.Resonance.Core Public API

namespace Ikon.Resonance.Core
  // One chunk of a speech event's audio: interleaved float samples plus the format and first/last markers, identified by the speech event's AudioChunk.Id. Mutable with settable properties and a parameterless constructor because the Teleport-generated serializer requires that shape — treat instances as immutable after construction.
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
