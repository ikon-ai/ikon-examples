namespace Ikon.App.Patterns.Examples;

// The audio and video guide, as one file that compiles.
//
// `Audio` and `Video` are accessors an app declares for itself, so the holder is the app class the
// guide is describing — its fence used to carry a second `[App] public partial class MyApp(…)`
// shell, which an assembly may declare exactly one of.
file sealed class AudioVideoGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:av-accessors
    private Audio Audio { get; } = new(app);
    private Video Video { get; } = new(app);
    #endregion

    private readonly Reactive<bool> _micBlocked = new(false);

    public async Task SendAsync(AudioChunk audioChunk, float[] samples, int sampleRate,
        int channelCount, bool isFirst, bool isLast, string streamId)
    {
        #region example:av-send
        // Every call returns an AudioPlayback at once; await its Completion to wait for playout.
        // Each client hears everything aimed at it, mixed.

        // 1. Speech — TTS in one call. A new line crossfades out the speech still playing.
        var line = Audio.Speak(MediaTargets.Everyone, "Hello world");
        await line.Completion;

        // 2. A clip (decoded file, generated music) — mixed with everything else, any length.
        //    Replace in a named slot cuts the previous clip there instead of overlapping it.
        Audio.Play(MediaTargets.Everyone, samples, sampleRate, channelCount,
            new PlayOptions { Mode = AudioMixMode.Replace, Slot = "music" });

        // 3. Audio produced as it goes (your own AudioChunks, a synth) — a live playback paces it.
        var live = Audio.PlayLive(MediaTargets.Everyone, audioChunk.SampleRate, audioChunk.ChannelCount);
        await live.WriteAsync(audioChunk.Samples);
        live.Complete();

        // 4. Raw — your own frames on your own stream id, UNMIXED and UNPACED: only for audio
        //    already produced in real time (echoing mic frames back out) or an engine that mixes
        //    and paces itself. Close the stream when done.
        await Audio.Raw.SendFrameAsync(MediaTargets.Everyone, streamId, samples, sampleRate, channelCount, isFirst, isLast);
        await Audio.Raw.CloseStreamAsync(streamId);
        #endregion
    }

    public void ReplyToSpeaker()
    {
        #region example:av-reply-to-speaker
        Audio.SpeechRecognizedAsync += async args =>
        {
            // Reply only to the person who spoke — NOT the whole room.
            Audio.Speak(MediaTargets.To([args.ClientSessionId]), $"You said: {args.Text}");
        };
        #endregion
    }

    public void MixerControl()
    {
        #region example:av-mixer-control
        Audio.Stop(MediaTargets.Everyone, "speech");                 // graceful: fade out all speech
        Audio.Stop(MediaTargets.To(7), "speech", fade: false);       // hard stop: silence speech for client 7 only
        Audio.Pause(MediaTargets.Everyone, "speech");                // hold speech where it is ...
        Audio.Resume(MediaTargets.Everyone, "speech");               // ... and carry on
        Audio.SetSlotVolume(MediaTargets.Everyone, "music", 0.3f);   // lower one slot, leave the rest
        Audio.Stop(MediaTargets.Everyone);                           // every slot, everyone
        #endregion
    }

    public void PushToTalk(UIView view)
    {
        #region example:av-push-to-talk
        view.PushToTalkButton(
            text: "Hold to talk",
            onPermissionChanged: async args =>
            {
                _micBlocked.Value = args.State != MediaPermissionState.Granted;
            });
        #endregion
    }

    public void AudioInput()
    {
        #region example:av-audio-input
        Audio.AudioInputStreamBeginAsync += async args =>
        {
            // Register per-stream state HERE — this fires before any frame from the stream.
            // args.StreamId, args.SampleRate, args.ChannelCount, args.ClientSessionId, args.UserId
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            // args.Samples: decoded float PCM in [-1, 1]; args.IsFirst / args.IsLast
            // bracket one captured segment (e.g. one push-to-talk press).
        };

        Audio.AudioInputStreamEndAsync += async args => { /* cleanup */ };
        #endregion
    }

    public void SpeechRecognition()
    {
        #region example:av-speech-recognition
        Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.SpeechRecognizedAsync += async args =>
        {
            // args.Text — the transcript; args.ClientSessionId / args.UserId — who spoke.
            // A per-client reactive scope is established automatically.
        };

        Audio.SpeechNotRecognizedAsync += async args =>
        {
            // args.Reason: NoAudio, Silence, NoSignal (a muted or virtual mic: tell the user to check which mic their device uses), NoText, or Error (failure in args.Error).
        };
        #endregion
    }

    public void TurnDetection()
    {
        #region example:av-turn-detection
        Audio.UseTurnDetection(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.TurnStartedAsync += async args => { /* listening indicator, barge-in hook */ };

        Audio.TurnSpeculativeAsync += async args =>
        {
            // The turn has PROBABLY ended; args.Text is the transcript so far. Start your
            // reply now with args.CancellationToken — it is cancelled if speech resumes.
        };

        Audio.SpeechRecognizedAsync += async args =>
        {
            // Confirms the turn. args.TurnId matches the started/speculative events
            // (it is 0 for push-to-talk recognitions from UseSpeechRecognition).
        };
        #endregion
    }

    public void SendChunk(float[] samples)
    {
        #region example:av-audio-chunk
        var chunk = new AudioChunk(
            id: Guid.NewGuid().ToString(),   // one unique id per utterance
            samples: samples,                 // float[] PCM in [-1, 1]
            sampleRate: 48000,
            channelCount: 1,
            isFirst: true,
            isLast: true);

        // Slot "speech" makes it count as the app speaking, replacing the line still playing
        Audio.Play(MediaTargets.Everyone, chunk.Samples, chunk.SampleRate, chunk.ChannelCount,
            new PlayOptions { Mode = AudioMixMode.Replace, Slot = "speech" });
        #endregion
    }

    #region example:av-group-mixer-fields
    private readonly GroupAudioMixer _mixer = new();

    // The frame event carries no SampleRate/ChannelCount — the format lives on the
    // BEGIN event, so stash it per stream:
    private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamFormats = new();
    #endregion

    public void GroupMixer(CancellationToken ct)
    {
        #region example:av-group-mixer
        // Wire participants and streams:
        app.OnClientJoined(async ctx => _mixer.AddParticipant(ctx.ClientSessionId));
        app.OnClientLeft(async ctx => _mixer.RemoveParticipant(ctx.ClientSessionId));

        Audio.AudioInputStreamBeginAsync += async args =>
        {
            _streamFormats[args.StreamId] = (args.SampleRate, args.ChannelCount);
            _mixer.AddStream(args.StreamId, args.ClientSessionId);   // tag the OWNING participant
        };

        Audio.AudioInputFrameAsync += async args =>
        {
            var format = _streamFormats[args.StreamId];
            _mixer.WriteSamples(args.StreamId, args.Samples, format.SampleRate, format.ChannelCount);
        };

        Audio.AudioInputStreamEndAsync += async args =>
        {
            _streamFormats.Remove(args.StreamId);
            _mixer.RemoveStream(args.StreamId);
        };

        // One pump forwards each personalized 20 ms frame to its participant. The frames
        // are already mixed and real-time paced, so the raw lane is correct here:
        _ = Task.Run(async () =>
        {
            await foreach (var (participantId, frame) in _mixer.StreamAsync(ct))
            {
                await Audio.Raw.SendFrameAsync(MediaTargets.To([participantId]), frame.StreamId, frame.Samples, frame.SampleRate,
                    frame.ChannelCount, frame.IsFirst, frame.IsLast);
            }
        });
        #endregion
    }

    #region example:av-video-streams-field
    // The frame event carries no codec or geometry — those arrive once on the BEGIN event,
    // so stash them per stream:
    private readonly Dictionary<string, VideoInputStreamBeginEventArgs> _videoStreams = new();
    #endregion

    public void ForwardVideo()
    {
        #region example:av-video-forward
        Video.VideoInputStreamBeginAsync += async args => _videoStreams[args.StreamId] = args;

        Video.VideoInputFrameAsync += async args =>
        {
            // args.Data is ENCODED codec bitstream (see the codec on the begin event), not pixels.
            // Forward it as-is — e.g. echo to everyone except the sender:
            var stream = _videoStreams[args.StreamId];
            var targets = app.Clients.Ids.Where(id => id != args.ClientSessionId).ToList();
            await Video.SendFrameAsync(MediaTargets.To(targets), args.Data, args.FrameNumber, args.IsKey,
                args.TimestampInUs, args.DurationInUs, stream.Codec, stream.Width, stream.Height,
                stream.Framerate, streamId: args.StreamId);
        };

        Video.VideoInputStreamEndAsync += async args =>
        {
            _videoStreams.Remove(args.StreamId);
            await Video.CloseAsync(args.StreamId);
        };
        #endregion
    }
}
