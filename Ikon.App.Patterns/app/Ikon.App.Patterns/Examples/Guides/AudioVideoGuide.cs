using Ikon.Common.Core.Protocol;

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
    private long _bytesReceived;
    private long _keyFramesReceived;

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

    #region example:av-video-tiles-field
    // One tile per camera: the surface its relay plays on, and the input its owner previews
    private readonly ReactiveList<CameraTile> _tiles = new();

    private sealed record CameraTile(string Surface, string InputId);
    #endregion

    public void RelayCameras()
    {
        #region example:av-video-relay
        Video.InputStartedAsync += async input =>
        {
            if (input.Kind != VideoSourceKind.Camera)
            {
                return;
            }

            // Each camera on a surface of its own, shown to everyone but its owner, later
            // joiners included: the owner previews its camera locally (localPreviewStreamId).
            // The platform forwards the frames, starts each viewer at a keyframe it asks the
            // camera for, and ends the playback (SourceEnded) when the camera stops.
            var surface = $"camera-{input.Id}";
            Video.Play(MediaTargets.EveryoneExcept(input.ClientSessionId), surface, input);
            _tiles.Add(new CameraTile(surface, input.Id));
        };

        Video.InputEndedAsync += async input =>
        {
            _tiles.RemoveAll(tile => tile.InputId == input.Id);
        };
        #endregion
    }

    public void CameraTiles(UIView view)
    {
        #region example:av-video-surface
        view.Row(["flex-wrap gap-2"], content: row =>
        {
            if (_tiles.Count == 0)
            {
                row.Text(["text-sm text-muted-foreground"], text: "No cameras on yet");
                return;
            }

            foreach (var tile in _tiles)
            {
                // The camera's owner sees its own capture locally, with no round trip;
                // everyone else sees the relay. The placeholder shows until the first frame.
                row.VideoSurface(["w-64 aspect-video rounded-lg bg-black"], surface: tile.Surface,
                    fit: VideoFit.Cover,
                    localPreviewStreamId: tile.InputId,
                    placeholder: tileView => tileView.Spinner(),
                    key: tile.Surface);
            }
        });
        #endregion
    }

    public async Task PlayEncodedFramesAsync(EncodedFrameSource encoder, CancellationToken ct)
    {
        #region example:av-video-live
        // Frames the app encodes itself (an ffmpeg process, a headless browser), in a codec the
        // viewers decode: H.264 plays on every WebRTC browser.
        await using var live = Video.PlayLive(MediaTargets.Everyone, "stage", VideoCodec.H264);

        // A viewer joined or lost frames and needs a keyframe: make the next frame one.
        live.KeyFrameRequestedAsync += async request => encoder.ForceKeyFrame();

        await foreach (var frame in encoder.ReadFramesAsync(ct))
        {
            // Frames go out in step with their timestamps. WriteAsync waits while half a second
            // is queued ahead of playout, and returns false once the playback has ended.
            if (!await live.WriteAsync(frame.Data, frame.IsKey, frame.Timestamp, ct))
            {
                break;
            }
        }

        live.Complete();   // plays out what is queued, then ends Finished
        await live.Completion;
        #endregion
    }

    public async Task PlayFfmpegAsync(CancellationToken ct)
    {
        #region example:av-video-ffmpeg
        // ffmpeg encodes, the platform only routes: H.264 Constrained Baseline as raw Annex-B on stdout
        var ffmpeg = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ffmpeg",
            "-f lavfi -i testsrc2=size=640x360:rate=30 -c:v libx264 -profile:v baseline -pix_fmt yuv420p " +
            "-preset veryfast -tune zerolatency -bf 0 -g 30 -x264-params repeat-headers=1 -f h264 pipe:1")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;

        await using var live = Video.PlayLive(MediaTargets.Everyone, "stage", VideoCodec.H264);

        try
        {
            // Splits the stream into frames (all slices of a picture together), stamps them
            // index / 30 and writes them in real time; false once the playback has ended
            await live.WriteH264StreamAsync(ffmpeg.StandardOutput.BaseStream, frameRate: 30, ct);
        }
        finally
        {
            ffmpeg.Kill();
        }
        #endregion
    }

    public void TapFrames()
    {
        #region example:av-video-frame-tap
        Video.InputStartedAsync += async input =>
        {
            // Every encoded frame of the input, off the message loop: the input's codec
            // bitstream (input.Codec), never decoded pixels.
            input.FrameReceivedAsync += async frame =>
            {
                Interlocked.Add(ref _bytesReceived, frame.Data.Length);

                if (frame.IsKey)
                {
                    Interlocked.Increment(ref _keyFramesReceived);
                }
            };

            // Asks the source for a keyframe now rather than at its next one.
            input.RequestKeyFrame();
        };
        #endregion
    }

    public async Task ShareScreenAsync(VideoInput screen, int hostId, IReadOnlyList<int> approvedIds)
    {
        #region example:av-video-audience
        // One playback, a changing audience: the presenter's screen, first to the host alone.
        var share = Video.Play(MediaTargets.To(hostId), "presentation", screen);

        // Clients who keep seeing it go on uninterrupted, new ones start at the next keyframe,
        // and clients left out have the surface cleared. An empty audience stops it.
        share.SetAudience(MediaTargets.To([hostId, .. approvedIds]));

        // Done: ends it and clears the surface for its viewers.
        share.Stop();
        var outcome = await share.Completion;   // VideoPlaybackOutcome.Stopped
        #endregion

        Log.Instance.Debug($"{outcome}");
    }
}

// What the live example assumes: the app's own encoder, handing out encoded frames in order
file sealed class EncodedFrameSource
{
    public void ForceKeyFrame()
    {
    }

    public async IAsyncEnumerable<(byte[] Data, bool IsKey, TimeSpan Timestamp)> ReadFramesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask;
        yield break;
    }
}
