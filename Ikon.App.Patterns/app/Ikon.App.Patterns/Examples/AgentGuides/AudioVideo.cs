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
        // Three ways to send audio — pick by how delivery is paced:

        // 1. Speech (TTS or AudioChunks through the speech mixer): real-time paced, new speech
        //    interrupts current speech with a fade. The default for spoken replies. The await
        //    returns once the utterance is queued — Audio.SpeakAndWaitAsync returns after playout.
        await Audio.SpeakAsync(MediaTargets.Everyone, text);
        Audio.SpeakChunk(MediaTargets.Everyone, audioChunk);

        // 2. Complete clip (decoded file, generated music): real-time paced, no mixer interruption.
        //    Await completes when the clip has been fully sent (≈ clip duration).
        await Audio.PlayClipAsync(MediaTargets.Everyone, samples, sampleRate, channelCount, streamId, cancellationToken: ct);

        // 3. Immediate, UNPACED transmit — only for audio already produced in real time (e.g. echoing
        //    mic frames back out) or very short clips. A long clip sent this way arrives all at once
        //    and can overflow client audio buffers — use PlayClipAsync for clips instead.
        await Audio.SendFrameAsync(MediaTargets.Everyone, samples, sampleRate, channelCount, isFirst, isLast, streamId);

        // Receive audio input from client microphone. args carry args.ClientContext /
        // args.ClientSessionId / args.UserId — use these directly; do NOT plumb state through
        // onCaptureStart to identify the client (use args.ClientSessionId in the handler instead).
        Audio.AudioInputStreamBeginAsync += async args => { /* args.StreamId, args.SampleRate, args.ClientSessionId */ };
        Audio.AudioInputFrameAsync += async args => { /* args.Samples, args.IsFirst, args.IsLast, args.ClientSessionId */ };
        Audio.AudioInputStreamEndAsync += async args => { /* cleanup */ };

        // For push-to-talk → chat, prefer the higher-level Audio.SpeechRecognizedAsync / PushToTalkButton —
        // see "AI Speech & Audio" section.

        // Stream info and cleanup
        var info = Audio.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, SampleRate, ChannelCount
        await Audio.CloseAsync(streamId);
        await Audio.CloseAllAsync();
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
        // over a stored clip's frames without pacing.
        await Video.SendFrameAsync(MediaTargets.Everyone, data, frameNumber, isKey, timestampInUs, durationInUs, codec, width, height, framerate, streamId);

        // Stream info and cleanup
        var info = Video.GetOutputStreamInfo(streamId); // StreamId, TrackId, Codec, Width, Height, Framerate
        await Video.CloseAsync(streamId);
        await Video.CloseAllAsync();
        #endregion

        Log.Instance.Debug($"{info}");
    }
}
