namespace Ikon.App
  // Two send lanes. Speak* shares one mixer: one utterance at a time for the whole app, and a new one fades out the old — it interrupts even between calls aimed at different clients. Send*/Play* are independent streams keyed by stream id and overlap freely.
  class Audio
    ctor(IAppBase app)
    AudioEncoderOptions? DefaultEncoderOptions { get; set; }
    AudioMetrics Metrics { get; }
    SpeechMixer SpeechMixer { get; }
    ValueTask CloseAllAsync()
    // streamId: The stream to close. Null closes the default stream
    ValueTask CloseAsync(string? streamId = null)
    AudioOutputStreamInfo? GetOutputStreamInfo(string? streamId = null)
    // How far the client has actually rendered the audio and whether the user can currently hear it. Null when the client has not reported yet (older SDKs never report). Reports arrive roughly twice per second while audio is playing; check AudioPlaybackStatus.ReceivedAtUtc for staleness.
    // clientSessionId: The client session id
    // streamId: The output stream. Null uses the default (speech mixer) stream
    AudioPlaybackStatus? GetPlaybackStatus(int clientSessionId, string? streamId = null)
    // Two concurrent calls on ONE stream id interleave their frames and corrupt playback — give each clip its own id. Cancelling after a frame went out closes the stream with a final end-of-stream frame; cancelling before the first frame sends nothing. Empty samples return without sending, and so does a clip no connected client would hear. App shutdown ends the clip quietly; only the caller's token throws.
    // streamId: Required when several streams run at once; null uses the default stream
    // encoderOptions: Null falls back to DefaultEncoderOptions
    // cancellationToken: Stops the clip early; the stream is closed when a frame was already sent
    Task PlayClipAsync(MediaTargets targets, ReadOnlyMemory<float> samples, int sampleRate, int channelCount, string? streamId = null, AudioEncoderOptions? encoderOptions = null, CancellationToken cancellationToken = default)
    // Unpaced — callers own the real-time pacing. Feed chunks as they are produced; a whole clip sent this way can overflow client buffers, so use PlayClipAsync for that. Sends nothing when no client the targets name is connected.
    // isFirst: True when this call carries the beginning of a clip (starts a new playback on the client)
    // isLast: True when this call carries the end of the clip (a single complete clip passes true for both)
    // streamId: Required when several streams run at once; null uses the default stream
    // encoderOptions: Null falls back to DefaultEncoderOptions
    ValueTask SendFrameAsync(MediaTargets targets, ReadOnlyMemory<float> samples, int sampleRate, int channelCount, bool isFirst, bool isLast, string? streamId = null, TimeSpan totalDuration = default, AudioEncoderOptions? encoderOptions = null)
    // Completes at end of mixer playout (pause-aware, real-time paced), not at end of generation. Long texts are backpressure-paced against the bounded mixer buffer, so any length is safe. An interruption by a newer Speak call completes the task quietly. Throws TimeoutException when the playout pipeline stops draining while unpaused — an app sequencing on speech must not continue as though the utterance had been heard. With nobody connected it returns at once without generating.
    // text: The text to speak. Whitespace-only text is a no-op
    // voice: Optional voice id. Null uses the model's default voice
    // instructions: Optional delivery instructions (tone, emotion, style). Support is model-specific; unsupported models ignore them
    // speed: Optional speaking speed, where 1.0 is normal (e.g. 0.8 is slower, 1.2 is faster). Null leaves the model's default. Support is model-specific; unsupported models ignore it
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAndWaitAsync(MediaTargets targets, string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, CancellationToken cancellationToken = default)
    // Also cancels the previous call's generation, not just its playback. Returns once the utterance is queued; await SpeakAndWaitAsync for playout. Throws TimeoutException when the playout pipeline stops draining while unpaused, so an abandoned utterance never reads as a spoken one. Returns without generating anything when no client the targets name is connected — speech is not bought for an empty room.
    // text: Whitespace-only text is a no-op
    // voice: Null uses the model's default voice
    // instructions: Delivery instructions (tone, emotion, style); unsupported models ignore them
    // speed: 1.0 is normal. Null leaves the model's default; unsupported models ignore it
    // cancellationToken: Cancels generation and playback of this utterance
    Task SpeakAsync(MediaTargets targets, string text, SpeechGeneratorModel model = ElevenFlash25, string? voice = null, string? instructions = null, double? speed = null, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null, CancellationToken cancellationToken = default)
    // Real-time paced by the speech mixer, so fast producers (typical TTS) cannot overflow client audio buffers; a chunk with a new id interrupts current playback with a fade. Returns immediately — playback happens in the background. Drops the chunk when no client the targets name is connected.
    void SpeakChunk(MediaTargets targets, AudioChunk audio, IReadOnlyList<IAudioEffect>? effects = null, IReadOnlyList<IAudioAnalyzer>? analyzers = null)
    // Call once during app setup. Mutually exclusive with UseTurnDetection, and calling it a second time throws — either conflict raises InvalidOperationException. A recognizer failure at segment time — a timestamp granularity the model does not support included — does not throw: SpeechNotRecognizedAsync fires with SpeechNotRecognizedReason.Error and the failure in its Error.
    // model: The speech recognizer model to use (e.g., WhisperLarge3Turbo).
    // silenceThresholdRms: RMS threshold below which the segment is treated as silence and skipped.
    // requireCorrelatedStream: When true (default), only fires for streams initiated through a CaptureButton (those with a CorrelationId). Set false to transcribe every audio stream including ad-hoc ones.
    // language: Optional language hint (e.g., "en", "fi"); empty string lets the model autodetect.
    // timeout: Per-segment recognition timeout.
    void UseSpeechRecognition(SpeechRecognizerModel model, float silenceThresholdRms = 0.01f, bool requireCorrelatedStream = true, string language = "", SpeechTimestamps timestamps = None, TimeSpan? timeout = null)
    // Call once during app setup. Mutually exclusive with UseSpeechRecognition, and calling it a second time throws — either conflict raises InvalidOperationException.
    // language: Language hint (e.g. "en", "fi"); empty lets the model autodetect.
    // config: Turn detector tuning; null uses defaults tuned for conversational voice.
    // speculative: Starts transcription at the probable turn end so a confirmed turn has zero added recognition latency.
    // pauseWhileAppSpeaking: Suppresses detection while the app is audibly speaking so its own voice can't trigger turns; set false for barge-in apps.
    // requireCorrelatedStream: Only detects turns on streams initiated through a CaptureButton (those with a CorrelationId); false detects on every stream.
    // timeout: Per-recognition timeout; null means one minute.
    void UseTurnDetection(SpeechRecognizerModel model = WhisperLarge3Turbo, string language = "", TurnDetectorConfig? config = null, bool speculative = true, bool pauseWhileAppSpeaking = true, bool requireCorrelatedStream = true, SpeechTimestamps timestamps = None, TimeSpan? timeout = null)
    // args.Samples are decoded float PCM at the sample rate from the stream's begin event; IsFirst/IsLast bracket one captured segment (e.g. one push-to-talk press).
    event AsyncEventHandler<AudioInputFrameEventArgs> AudioInputFrameAsync
    // Handlers may set args.StreamingMode to control when the stream's frames are delivered (streamed live, or buffered until the total duration is known / until the last frame).
    event AsyncEventHandler<AudioInputStreamBeginEventArgs> AudioInputStreamBeginAsync
    event AsyncEventHandler<AudioInputStreamEndEventArgs> AudioInputStreamEndAsync
    // Reports arrive periodically while a stream is active and immediately on state changes; GetPlaybackStatus holds the latest snapshot per client.
    event AsyncEventHandler<AudioPlaybackReportEventArgs> PlaybackReportReceivedAsync
    // Exactly one of this and SpeechRecognizedAsync fires per completed segment, and per detected turn — carrying that turn's SpeechNotRecognizedEventArgs.TurnId — so every TurnStartedAsync is closed (neither fires once the app is shutting down). An app that latches busy state when capture stops — a "Transcribing..." spinner, a disabled button — must release it here as well as in SpeechRecognizedAsync; handling only the success event leaves that state stuck on for any press that produces no speech.
    event AsyncEventHandler<SpeechNotRecognizedEventArgs> SpeechNotRecognizedAsync
    // Fires only after UseSpeechRecognition or UseTurnDetection has been called once at setup; subscribing without one of those means this event never fires.
    event AsyncEventHandler<SpeechRecognizedEventArgs> SpeechRecognizedAsync
    // Fires only after UseTurnDetection has been called once at setup. Start downstream work (e.g. generating a reply) with the args' cancellation token: it is cancelled if the user resumes speaking; otherwise SpeechRecognizedAsync confirms the turn with the same TurnSpeculativeEventArgs.TurnId.
    event AsyncEventHandler<TurnSpeculativeEventArgs> TurnSpeculativeAsync
    // Fires only after UseTurnDetection has been called once at setup. A barge-in or listening-indicator hook.
    event AsyncEventHandler<TurnStartedEventArgs> TurnStartedAsync
  class AudioInputFrameEventArgs : EventArgs
    ctor(string streamId, Context clientContext, float[] samples, bool isFirst, bool isLast, TimeSpan totalDuration, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    bool IsFirst { get; }
    bool IsLast { get; }
    // Decoded PCM samples in range [-1.0, 1.0]
    float[] Samples { get; }
    string StreamId { get; }
    TimeSpan TotalDuration { get; set; }
    string UserId { get; }
  class AudioInputStreamBeginEventArgs : EventArgs
    ctor(string streamId, string description, string sourceType, int sampleRate, int channelCount, Context clientContext, int trackId, string? correlationId)
    int ChannelCount { get; }
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Set by the originating CaptureButton; null for ad-hoc streams.
    string? CorrelationId { get; }
    string Description { get; }
    int SampleRate { get; }
    string SourceType { get; }
    string StreamId { get; }
    AudioInputStreamingMode StreamingMode { get; set; }
    int TrackId { get; }
    string UserId { get; }
  class AudioInputStreamEndEventArgs : EventArgs
    ctor(string streamId, Context clientContext, string? correlationId)
    Context ClientContext { get; }
    int ClientSessionId { get; }
    // Inherited from the AudioStreamBegin (set by the originating CaptureButton); null for ad-hoc streams.
    string? CorrelationId { get; }
    string StreamId { get; }
    string UserId { get; }
  record AudioOutputStreamInfo
    ctor(string StreamId, int TrackId, AudioCodec Codec, int SampleRate, int ChannelCount)
    int ChannelCount { get; init; }
    AudioCodec Codec { get; init; }
    int SampleRate { get; init; }
    string StreamId { get; init; }
    int TrackId { get; init; }
  class AudioPlaybackReportEventArgs : EventArgs
    ctor(AudioPlaybackStatus status)
    AudioPlaybackStatus Status { get; }
  sealed class AudioPlaybackStatus
    ctor()
    TimeSpan BufferedDuration { get; init; }
    int ClientSessionId { get; init; }
    uint Epoch { get; init; }
    // Null when the client cannot observe the playout position (e.g. WebRTC playback)
    TimeSpan? PlayedDuration { get; init; }
    DateTime ReceivedAtUtc { get; init; }
    AudioPlaybackState State { get; init; }
    int TrackId { get; init; }
  // A held scope reports the session's idle time as zero, which every tier of the backend's reaping measures against, so while one is open nothing reaps the instance at all. That is what the scope is for and it is also how an instance ends up running unattended for a night — so a scope holds for a stated duration and no longer. State one with StartAsync for work that runs longer than DefaultHold, or extend the scope while it runs.
  class BackgroundWork
    // Calls are ref-counted: the server is notified only on the first StartAsync and the last StopAsync. Dispose the returned scope (or call StopAsync) to release. A scope left undisposed stops holding after DefaultHold and says so in the log — state a longer duration for work that needs one rather than relying on that.
    ValueTask<BackgroundWorkScope> StartAsync()
    // The hold is released when the scope is disposed or when expectedDuration passes, whichever comes first, so work that overruns can be reaped rather than freezing the idle clock indefinitely; call BackgroundWorkScope.ExtendAsync from work that is still making progress. Durations above MaxHold are clamped.
    // expectedDuration: How long the work should take. Must be positive
    // reason: What the work is, named in the log when the hold expires or is extended
    // throws ArgumentOutOfRangeException: The duration is zero or negative
    ValueTask<BackgroundWorkScope> StartAsync(TimeSpan expectedDuration, string reason)
    ValueTask StopAsync()
    // Covers any ordinary job an app runs behind a closed tab, and bounds what a scope nobody ever releases costs.
    static readonly TimeSpan DefaultHold
    // A hold longer than the platform's own longest idle budget is indistinguishable from one that never ends. A longer duration is clamped to this and the clamp is logged.
    static readonly TimeSpan MaxHold
  // Disposing twice is safe, and so is disposing one that has already expired.
  sealed class BackgroundWorkScope : IAsyncDisposable
    ValueTask DisposeAsync()
    // For work that is still making progress past the duration it was started for. Call it from the work itself — a loop that extends on its own, with nothing to report, is the hold that never ends wearing a different hat. Extending a released scope does nothing.
    // additionalDuration: How much longer the work needs. Must be positive
    // throws ArgumentOutOfRangeException: The duration is zero or negative
    ValueTask ExtendAsync(TimeSpan additionalDuration)
  // Every null property leaves that setting to the client. Start from Default and override what you need.
  sealed record ClientAudioCaptureOptions
    ctor()
    bool? AutoGainControl { get; init; }
    int? Bitrate { get; init; }
    // 32 kbit/s, auto gain control and noise suppression on, echo cancellation off (nothing is being played back in the common server-transcription case); device is left to the client.
    static ClientAudioCaptureOptions Default { get; }
    string? DeviceId { get; init; }
    // Needed for two-way calls on a loudspeaker; pointless — and lossy — when nothing is being played back, which is why Default leaves it off.
    bool? EchoCancellation { get; init; }
    bool? NoiseSuppression { get; init; }
  sealed record ClientContact
    // Names: The contact's names.
    // Emails: The contact's email addresses.
    // Phones: The contact's phone numbers.
    ctor(IReadOnlyList<string> Names, IReadOnlyList<string> Emails, IReadOnlyList<string> Phones)
    IReadOnlyList<string> Emails { get; init; }
    IReadOnlyList<string> Names { get; init; }
    IReadOnlyList<string> Phones { get; init; }
