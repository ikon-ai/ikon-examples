using Ikon.Common.Core.Protocol;

namespace Ikon.App.Patterns.Examples;

// `Audio` and `Video` are accessors an app declares for itself, so each holder is the app class the
// docs are talking about — the declaration inside the first region is the line a reader adds.
file sealed class AudioExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:audio-accessor
    private Audio Audio { get; } = new(app);
    #endregion

    public async Task RunAsync(string text, AudioChunk audioChunk, float[] samples, int sampleRate,
        int channelCount, string streamId, bool isFirst, bool isLast, CancellationToken ct)
    {
        #region example:audio-usage
        // Every play call returns an AudioPlayback at once; each client hears everything aimed at
        // it, mixed. Await playback.Completion to wait for playout (it never throws).

        // 1. Speech: TTS in one call. A new line crossfades out the speech still playing (slot "speech").
        var line = Audio.Speak(MediaTargets.Everyone, text);
        await line.Completion;

        // 2. A clip (decoded file, synthesized sound): mixed with everything else, so clips overlap.
        //    A slot with Replace cuts the previous clip in that slot instead.
        Audio.Play(MediaTargets.Everyone, samples, sampleRate, channelCount);
        Audio.Play(MediaTargets.Everyone, samples, sampleRate, channelCount,
            new PlayOptions { Mode = AudioMixMode.Replace, Slot = "music", Loop = true });

        // 2a. Music or a long recording from a URL: decoded as it plays, looped by decoding it again.
        Audio.Play(MediaTargets.Everyone, new Uri("https://cdn.example.com/theme.mp3"),
            new PlayOptions { Mode = AudioMixMode.Replace, Slot = "music", Loop = true });

        // 2b. A short fixed sound replayed often (a pad, a click, a notification): make it once.
        //     A client that can cache it plays its own copy, without the streaming delay; others hear it mixed.
        var hit = Audio.CreateSound(new AudioClip(samples, sampleRate, channelCount));
        Audio.Play(MediaTargets.Everyone, hit);

        // 3. Audio the app produces as it goes (a synth, a relayed model, streamed chunks): write it
        //    into a live playback, which paces it. WriteAsync waits while the buffer is full and
        //    returns false once the playback has ended, so `while (await live.WriteAsync(...))` is the loop.
        var live = Audio.PlayLive(MediaTargets.Everyone, audioChunk.SampleRate, audioChunk.ChannelCount);
        await live.WriteAsync(audioChunk.Samples, ct);
        live.Complete();   // no more writes: plays out what is buffered, then ends

        // Control by slot, or through the handle.
        Audio.Stop(MediaTargets.Everyone, "speech");
        Audio.SetSlotVolume(MediaTargets.Everyone, "music", 0.5f, fade: TimeSpan.FromSeconds(1));
        line.Stop();

        // 4. Raw: your own frames on your own stream id, sent at once, UNMIXED and UNPACED — only
        //    for audio already produced in real time (e.g. echoing mic frames back out) or an
        //    engine that mixes and paces itself.
        await Audio.Raw.SendFrameAsync(MediaTargets.Everyone, streamId, samples, sampleRate, channelCount, isFirst, isLast);

        // Receive audio input from client microphone. args carry args.ClientContext /
        // args.ClientSessionId / args.UserId — use these directly; do NOT plumb state through
        // onCaptureStart to identify the client (use args.ClientSessionId in the handler instead).
        Audio.AudioInputStreamBeginAsync += async args => { /* args.StreamId, args.SampleRate, args.ClientSessionId */ };
        Audio.AudioInputFrameAsync += async args => { /* args.Samples, args.IsFirst, args.IsLast, args.ClientSessionId */ };
        Audio.AudioInputStreamEndAsync += async args => { /* cleanup */ };

        // For push-to-talk → chat, prefer the higher-level Audio.SpeechRecognizedAsync / PushToTalkButton —
        // see "AI Speech & Audio" section.

        // Raw stream info and cleanup
        var info = Audio.Raw.GetStreamInfo(streamId); // StreamId, TrackId, Codec, SampleRate, ChannelCount
        await Audio.Raw.CloseStreamAsync(streamId);
        #endregion

        Log.Instance.Debug($"{info}");
    }
}

file sealed class VideoExamples(IApp<SessionIdentity, ClientParameters> app)
{
    #region example:video-accessor
    private Video Video { get; } = new(app);
    #endregion

    public async Task RunAsync(UIView view, VideoInput camera, VideoInput screen, int hostId, byte[] encoded, bool isKey,
        TimeSpan timestamp, string streamId, int width, int height, Stream ffmpegStdout, CancellationToken ct)
    {
        #region example:video-usage
        // The platform routes ENCODED frames and never decodes or encodes them. A surface is a
        // named place on clients where one picture shows; a new playback on it replaces the last.

        // 1. Relay a client's camera or screen: one call per input, not per frame. Each viewer
        //    starts at a keyframe the platform asks the source for; the playback ends
        //    SourceEnded when the input does. Handlers run in the sending client's scope.
        Video.InputStartedAsync += async input =>
        {
            // input.Id, input.Kind (Camera/Screen/Other), input.ClientSessionId, input.Codec, input.Width
            // Everyone but the owner, who previews its camera locally; later joiners included
            Video.Play(MediaTargets.EveryoneExcept(input.ClientSessionId), $"camera-{input.Id}", input);
        };
        Video.InputEndedAsync += async input => { /* drop the tile; its playback ends on its own, maybe just after */ };

        // 2. Show a surface. localPreviewStreamId (a VideoInput's Id) shows the owner its own
        //    capture without the round trip; everyone else sees the surface.
        view.VideoSurface(["aspect-video w-full rounded-lg bg-black"], surface: $"camera-{camera.Id}",
            fit: VideoFit.Cover, localPreviewStreamId: camera.Id,
            placeholder: p => p.Text(["text-sm text-muted-foreground"], text: "Waiting for video"));

        // 3. A changing audience: change it in place rather than starting another playback.
        // Everyone and EveryoneExcept(ids) include later joiners; To(ids) is fixed.
        var share = Video.Play(MediaTargets.To(hostId), "presentation", screen);
        share.SetAudience(MediaTargets.Everyone);   // clients left out of a new audience are cleared
        var outcome = await share.Completion;       // never throws: Stopped, Replaced, SourceEnded, ...

        // 4. Frames the app encodes itself (ffmpeg, a headless browser): a live playback paces
        //    them by timestamp. Force a keyframe in your encoder when a viewer asks for one.
        var live = Video.PlayLive(MediaTargets.Everyone, "stage", VideoCodec.H264);
        live.KeyFrameRequestedAsync += async request => { /* make the encoder's next frame a keyframe */ };
        await live.WriteAsync(encoded, isKey, timestamp, ct);   // one picture, all its slices; false once ended
        await live.WriteH264StreamAsync(ffmpegStdout, frameRate: 30, ct);   // or a whole H.264 stream, split for you
        live.Complete();                                         // plays out what is queued, then ends

        // 5. Inspect an input's frames (health counters, recording): the encoded bitstream.
        camera.FrameReceivedAsync += async frame => { /* frame.Data, frame.IsKey, frame.Timestamp */ };
        camera.RequestKeyFrame();

        // 6. Raw: your own stream id, UNPACED and ungated, for engines that manage streams and
        //    keyframes themselves. Render it with view.VideoStreamCanvas(streamId: ...).
        await Video.Raw.SendFrameAsync(MediaTargets.Everyone, streamId, encoded, isKey, timestamp, VideoCodec.H264, width, height);
        await Video.Raw.CloseStreamAsync(streamId);

        // Stop: one playback, a surface, or everything this Video holds.
        share.Stop();
        Video.Stop(MediaTargets.Everyone, "stage");
        #endregion

        Log.Instance.Debug($"{outcome}");
    }
}
