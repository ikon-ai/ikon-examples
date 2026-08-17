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
  sealed class AudioEncoderOptions
    ctor(int? bitrate = null, bool? useVBR = null, int? complexity = null)
    int? Bitrate { get; }
    int? Complexity { get; }
    bool? UseVBR { get; }
  enum AudioInputStreamingMode
    Streaming
    DelayUntilTotalDurationKnown
    DelayUntilIsLast
  static class MuLawCodec
    static float[] Decode(ReadOnlySpan<byte> muLaw)
    static byte[] Encode(ReadOnlySpan<float> samples)
