namespace Ikon.AI.MusicGeneration
  interface IMusicGenerator : IDisposable, IMusicGeneratorInfo
    int ChannelCount { get; }
    int SampleRate { get; }
    // Requires IMusicGeneratorInfo.SupportsStreaming; otherwise throws NonRetryableAIException. Use GenerateMusicFileAsync for a buffered encoded file.
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
  interface IMusicGeneratorInfo
    // When false the model ignores MusicGeneratorConfig.DurationSeconds, emitting a fixed-length clip or (when editing) matching the input clip's length.
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    // When false, IMusicGenerator.GenerateMusicAsync throws; use the buffered IMusicGenerator.GenerateMusicFileAsync instead.
    bool SupportsStreaming { get; }
  sealed class MusicGenerator : IMusicGenerator
    ctor(string modelName, IReadOnlyList<ModelRegion>? regions = null)
    ctor(MusicGeneratorModel model, IReadOnlyList<ModelRegion>? regions = null)
    int ChannelCount { get; }
    int SampleRate { get; }
    bool SupportsDurationControl { get; }
    bool SupportsEditing { get; }
    bool SupportsStreaming { get; }
    void Dispose()
    Task<MusicGeneratorResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    // Static one-shot; constructs and disposes a MusicGenerator per call. Defaults to MusicGeneratorModel.ElevenLabsMusicV2 (supports duration control and editing); override via model. Returns a buffered, encoded audio file (.Data/.MimeType/.DurationSeconds). Use the constructor + GenerateMusicFileAsync for duration/input-audio/seed, or GenerateMusicAsync for streaming PCM chunks.
    static Task<MusicGeneratorResult> GenerateAsync(string prompt, MusicGeneratorModel model = ElevenLabsMusicV2, CancellationToken cancellationToken = default)
    IAsyncEnumerable<AudioChunk> GenerateMusicAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    Task<MusicGeneratorResult> GenerateMusicFileAsync(MusicGeneratorConfig config, CancellationToken cancellationToken = default)
    static MusicGeneratorCapabilities GetCapabilities(MusicGeneratorModel model)
    static IReadOnlyList<ModelRegion> GetSupportedRegions(MusicGeneratorModel model)
  sealed class MusicGeneratorCapabilities : IMusicGeneratorInfo
    ctor()
    bool SupportsDurationControl { get; init; }
    bool SupportsEditing { get; init; }
    bool SupportsStreaming { get; init; }
  // With an empty InputAudios the model generates from the prompt alone; with one or more it performs audio-to-audio editing (the prompt re-styles the clips, timing preserved). The underlying music model works on clips of at least 3 seconds. For shorter UI/game sound effects use SoundEffectGenerator instead.
  sealed record MusicGeneratorConfig
    ctor()
    // Seconds, clamped to the model's supported range. When editing, set it to the source clip's length to keep the original timing. Ignored unless IMusicGeneratorInfo.SupportsDurationControl is true.
    double? DurationSeconds { get; init; }
    bool ForceInstrumental { get; init; }
    List<InputAudio> InputAudios { get; init; }
    string Prompt { get; init; }
    // Applies to the buffered IMusicGenerator.GenerateMusicFileAsync result; the streaming IMusicGenerator.GenerateMusicAsync chunks are unaffected.
    ResultDelivery ResultDelivery { get; init; }
    int Seed { get; init; }
    TimeSpan Timeout { get; init; }
  enum MusicGeneratorModel
    ElevenLabsMusicV2
    FalStableAudio
    FalLyria2
    // The platform provides the Suno key, so these behave like every other model here and need no per-app secret. An app may still override it with its own subscription by setting IKON_SUNO_API_KEY (ikon app secret set IKON_SUNO_API_KEY <key>), which is then billed as bring-your-own-key usage.
    SunoV5
    SunoV55
  static class MusicGeneratorModelExtensions
    static string DisplayName(this MusicGeneratorModel model)
  // Kind tells how the audio was delivered: inline bytes in Data, or a signed download URL in Url valid for roughly one hour.
  sealed record MusicGeneratorResult : IResultPayload
    ctor()
    byte[]? Data { get; init; }
    double DurationSeconds { get; init; }
    ResultKind Kind { get; init; }
    string MimeType { get; init; }
    string? Url { get; init; }
