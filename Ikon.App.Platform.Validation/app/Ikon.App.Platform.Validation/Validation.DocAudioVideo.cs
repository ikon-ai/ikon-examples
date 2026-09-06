// The audio and video guide, as one file that compiles.
//
// `Audio` and `Video` are accessors an app declares for itself, so the holder is the app class the
// guide is describing — its fence used to carry a second `[App] public partial class MyApp(…)`
// shell, which an assembly may declare exactly one of.
file sealed class DocAudioVideoGuide(IApp<SessionIdentity, ClientParams> app)
{
    #region docsnippet:av-accessors
    private Audio Audio { get; } = new(app);
    private Video Video { get; } = new(app);
    #endregion

    private readonly Reactive<bool> _micBlocked = new(false);

    public async Task SendAsync(AudioChunk audioChunk, float[] samples, int sampleRate,
        int channelCount, bool isFirst, bool isLast, string streamId)
    {
        #region docsnippet:av-send
        // 1. Speech — real-time paced through the speech mixer; new speech interrupts
        //    current speech with a fade. The default for spoken replies.
        await Audio.SpeakAsync(MediaTargets.Everyone, "Hello world");                       // TTS in one call
        Audio.SpeakChunk(MediaTargets.Everyone, audioChunk);                                // your own AudioChunks

        // 2. Complete clip (decoded file, generated music) — real-time paced, no
        //    interruption semantics. Safe for any length.
        await Audio.PlayClipAsync(MediaTargets.Everyone, samples, sampleRate, channelCount, streamId: "music");

        // 3. Immediate, UNPACED — only for audio already produced in real time (echoing
        //    mic frames back out) or very short clips. A long clip sent this way arrives
        //    all at once and can overflow client audio buffers; use PlayClipAsync for clips.
        await Audio.SendFrameAsync(MediaTargets.Everyone, samples, sampleRate, channelCount, isFirst, isLast, streamId);
        #endregion
    }

    public void ReplyToSpeaker()
    {
        #region docsnippet:av-reply-to-speaker
        Audio.SpeechRecognizedAsync += async args =>
        {
            // Reply only to the person who spoke — NOT the whole room.
            await Audio.SpeakAsync(MediaTargets.To([args.ClientSessionId]), $"You said: {args.Text}");
        };
        #endregion
    }

    public void MixerControl()
    {
        #region docsnippet:av-mixer-control
        Audio.SpeechMixer.FadeOut();   // graceful: fade out the current utterance
        Audio.SpeechMixer.Clear();     // hard reset: discard current, pending, and paused speech
        #endregion
    }

    public void PushToTalk(UIView view)
    {
        #region docsnippet:av-push-to-talk
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
        #region docsnippet:av-audio-input
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
        #region docsnippet:av-speech-recognition
        Audio.UseSpeechRecognition(SpeechRecognizerModel.WhisperLarge3Turbo);

        Audio.SpeechRecognizedAsync += async args =>
        {
            // args.Text — the transcript; args.ClientSessionId / args.UserId — who spoke.
            // A per-client reactive scope is established automatically.
        };

        Audio.SpeechNotRecognizedAsync += async args =>
        {
            // args.Reason: NoAudio, Silence, NoText, or Error (failure in args.Error).
        };
        #endregion
    }

    public void TurnDetection()
    {
        #region docsnippet:av-turn-detection
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
        #region docsnippet:av-audio-chunk
        var chunk = new AudioChunk(
            id: Guid.NewGuid().ToString(),   // one unique id per utterance
            samples: samples,                 // float[] PCM in [-1, 1]
            sampleRate: 48000,
            channelCount: 1,
            isFirst: true,
            isLast: true);
        Audio.SpeakChunk(MediaTargets.Everyone, chunk);
        #endregion
    }

    #region docsnippet:av-group-mixer-fields
    private readonly GroupAudioMixer _mixer = new();

    // The frame event carries only StreamId/Samples/IsFirst/IsLast — the format lives on the
    // BEGIN event, so stash it per stream:
    private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamFormats = new();
    #endregion

    public void GroupMixer(CancellationToken ct)
    {
        #region docsnippet:av-group-mixer
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
        // are already real-time paced, so SendFrameAsync is correct here:
        _ = Task.Run(async () =>
        {
            await foreach (var (participantId, frame) in _mixer.StreamAsync(ct))
            {
                await Audio.SendFrameAsync(MediaTargets.To([participantId]), frame.Samples, frame.SampleRate, frame.ChannelCount,
                    frame.IsFirst, frame.IsLast, frame.StreamId);
            }
        });
        #endregion
    }

    #region docsnippet:av-video-streams-field
    // The frame event carries no codec or geometry — those arrive once on the BEGIN event,
    // so stash them per stream:
    private readonly Dictionary<string, VideoInputStreamBeginEventArgs> _videoStreams = new();
    #endregion

    public void ForwardVideo()
    {
        #region docsnippet:av-video-forward
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
