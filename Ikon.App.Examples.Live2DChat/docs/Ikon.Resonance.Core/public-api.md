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
