namespace Ikon.Resonance
  // Immutable — the mixer captures these values at construction; build a new config (and mixer) to change them.
  sealed record SpeechMixerConfig
    ctor()
    CrossfadeCurve CrossfadeCurve { get; init; }
    double EndPaddingMs { get; init; }
    double FadeInMs { get; init; }
    FadeMode FadeMode { get; init; }
    double FadeOutMs { get; init; }
    // How long (ms, default 30000) a speech event whose producer stopped feeding it without ever sending IsLast keeps emitting silence before the mixer finalizes it. The cut is logged as a warning and the event's waiter resolves as SpeechEventOutcome.Abandoned. Generous by default so a slow streaming producer is never cut mid-utterance.
    double IdleFinalizeMs { get; init; }
    // Upper bound only; the queue grows on demand from a small size. Samples added beyond this bound are dropped with a throttled warning, never thrown.
    double MaxBufferSizeMs { get; init; }
    // Caps effect tail padding in case an effect's output never decays below PaddingThreshold.
    double MaxPaddingTimeMs { get; init; }
    // RMS threshold below which effect tail padding stops. Default is 0.001 (~-60 dB), meaning padding continues until output is essentially silent.
    double PaddingThreshold { get; init; }
  // Immutable — construct a new config (and detector) instead of mutating a shared instance.
  sealed record TurnDetectorConfig
    ctor()
    // Tuning for the built-in level gate and the onset pre-buffer (SilenceRemoverConfig.PreBufferMs). Only the level-tracking and pre-buffer fields apply; the onset/trailing fields belong to SilenceRemover. When null, SilenceRemover defaults are used except SilenceRemoverConfig.ReleaseAlpha is raised to 0.3 — turn detection needs the level to fall promptly when speech stops (the hold-through-pauses role is played by TurnEndSilence instead), where the slow default would add noticeable latency to every turn end.
    SilenceRemoverConfig? GateConfig { get; init; }
    // Maximum turn length. A confirmed turn still running at this point is force-ended (TurnEventKind.TurnEnded with TurnEvent.Truncated set); an unconfirmed candidate is discarded without events. Buffered audio never exceeds this. Must be longer than MinSpeechDuration.
    TimeSpan MaxTurnDuration { get; init; }
    // Minimum cumulative speech required before a turn is confirmed. Shorter bursts (coughs, clicks) are discarded without producing any events.
    TimeSpan MinSpeechDuration { get; init; }
    // Silence duration after which the turn has probably ended and a TurnEventKind.SpeculativeTurnEnd fires, letting downstream work start before the turn end is certain. Must be shorter than TurnEndSilence. Null disables speculative turn ends.
    TimeSpan? SpeculativeSilence { get; init; }
    // Optional external speech classifier (e.g. a neural VAD such as Silero) that replaces the built-in adaptive level gate. Receives one chunk of interleaved float PCM and returns whether it contains speech. Null uses the built-in gate.
    Func<ReadOnlyMemory<float>, bool>? SpeechClassifier { get; init; }
    // Silence duration that ends a turn. This window — not the level gate — provides the "hold through natural pauses" behavior, so mid-sentence breaths don't split a turn.
    TimeSpan TurnEndSilence { get; init; }
  // Samples are written incrementally; the WAV header is finalized when the file is first accessed, after which adding samples throws.
  class WavFile : IDisposable
    // sampleRate: The sample rate in Hz (e.g., 44100, 48000).
    // channelCount: The number of audio channels (1 for mono, 2 for stereo).
    // sampleFormat: The sample format to use for the WAV file.
    ctor(int sampleRate, int channelCount, WavFile.SampleFormat sampleFormat)
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Short.
    void AddSamples(ReadOnlySpan<short> samples)
    // samples: The interleaved audio samples to add.
    // throws InvalidOperationException: Thrown if the file has been finalized or the sample format is not Float.
    void AddSamples(ReadOnlySpan<float> samples)
    byte[] AsArray()
    // Gets the WAV file as a fresh readable stream over a copy of the data. The returned stream is independent of this WavFile, so it survives disposal of the builder and each call returns its own stream.
    Stream AsStream()
    void Dispose()
    // filePath: The path where the WAV file will be saved.
    void SaveToFile(string filePath)
  enum WavFile.SampleFormat
    Short
    Float
