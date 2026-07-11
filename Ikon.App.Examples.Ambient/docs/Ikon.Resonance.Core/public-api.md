# Ikon.Resonance.Core Public API

namespace Ikon.Resonance.Core
  class AudioChunk
    ctor()
    ctor(string id, float[] samples, int sampleRate, int channelCount, bool isFirst, bool isLast)
    int ChannelCount { get; set; }
    string Id { get; set; }
    bool IsFirst { get; set; }
    bool IsLast { get; set; }
    int SampleRate { get; set; }
    float[] Samples { get; set; }
  // Codec-agnostic audio encoding options.
  class AudioEncoderOptions
    ctor()
    // Target bitrate in bits per second (e.g., 128000 for 128 kbps).
    int? Bitrate { get; set; }
    // Encoder complexity/quality level. Higher = better quality, more CPU. Interpretation is codec-specific (e.g., 0-10 for Opus).
    int? Complexity { get; set; }
    // Enable variable bitrate encoding.
    bool? UseVBR { get; set; }
  // Controls when incoming audio frames are output to listeners
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
