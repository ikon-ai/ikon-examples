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

    public async Task RunAsync(byte[] data, int frameNumber, bool isKey, ulong timestampInUs,
        uint durationInUs, VideoCodec codec, int width, int height, int framerate, string streamId)
    {
        #region example:video-usage
        // Receive video input from client camera/screen
        Video.VideoInputStreamBeginAsync += async args => { /* args.StreamId, args.Codec, args.Width, args.Height */ };
        Video.VideoInputFrameAsync += async args => { /* args.Data, args.FrameNumber, args.IsKey */ };
        Video.VideoInputStreamEndAsync += async args => { /* cleanup */ };

        // Forward/echo video to other clients. Frames are transmitted immediately — call once per
        // frame at the source framerate (e.g. forward each incoming frame as it arrives); never loop
        // over a stored clip's frames without pacing. When forwarding a capture, pass its args.StreamId
        // so a VideoStreamCanvas showing that id plays it.
        await Video.SendFrameAsync(MediaTargets.Everyone, data, frameNumber, isKey, timestampInUs, durationInUs, codec, width, height, framerate, streamId);

        // Stream info and cleanup
        var info = Video.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, Width, Height, Framerate
        await Video.CloseAsync(streamId);
        await Video.CloseAllAsync();
        #endregion

        Log.Instance.Debug($"{info}");
    }
}
