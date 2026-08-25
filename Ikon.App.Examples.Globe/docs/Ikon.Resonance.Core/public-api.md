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
