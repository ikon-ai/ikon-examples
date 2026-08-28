# Ikon Audio & Video Guide

How an Ikon AI app's C# app class plays audio to clients, receives microphone and camera streams, transcribes speech, and mixes group calls. Read this if your app makes sound, listens, or handles video.

## Setup: construct the services in a field initializer

`Audio` and `Video` are app services. Declare each as a property initialized from the app parameter of your app class's primary constructor:

```csharp
[App]
public partial class MyApp(IApp<SessionIdentity, ClientParameters> app)
{
    private Audio Audio { get; } = new(app);
    private Video Video { get; } = new(app);
}
```

**The field initializer is mandatory, not a style choice.** The `Audio` constructor subscribes to the app's `StartingAsync` event, and the speech-mixer pump — the loop that actually plays everything sent through `SpeakAsync` and `SendSpeech` — is only started from that handler. Field initializers run before the framework raises `StartingAsync`; code inside `Main()` runs after it. An `Audio` constructed in `Main()` has missed the event, its pump never starts, and every `SpeakAsync` call is silently swallowed — no exception, no sound.

All Ikon namespaces are auto-imported through the app scaffold's `GlobalUsings.cs`, so no `using` directives are needed for any type in this guide.

## Sending audio: three ways, chosen by pacing

```csharp
// 1. Speech — real-time paced through the speech mixer; new speech interrupts
//    current speech with a fade. The default for spoken replies.
await Audio.SpeakAsync("Hello world");                       // TTS in one call
Audio.SendSpeech(audioChunk);                                // your own AudioChunks

// 2. Complete clip (decoded file, generated music) — real-time paced, no
//    interruption semantics. Safe for any length.
await Audio.StreamAsync(samples, sampleRate, channelCount, streamId: "music");

// 3. Immediate, UNPACED — only for audio already produced in real time (echoing
//    mic frames back out) or very short clips. A long clip sent this way arrives
//    all at once and can overflow client audio buffers; use StreamAsync for clips.
await Audio.SendImmediateAsync(samples, sampleRate, channelCount, isFirst, isLast, streamId);
```

`SpeakAsync` returns when the utterance is queued; `SpeakAndWaitAsync` completes when playout finishes (an interruption by a newer call completes it quietly). Both take optional `model` (default `SpeechGeneratorModel.ElevenFlash25`), `voice`, `instructions`, and `speed`. To generate speech *without* playing it, use the one-shot `await SpeechGenerator.GenerateAsync(text)`, which returns a PCM `AudioChunk`.

Don't run two concurrent `StreamAsync` calls on the same stream id — the interleaved frames corrupt client playback. Use distinct stream ids or await the previous call first.

### `targetIds`: null means everyone

Every send method takes `IReadOnlyList<int>? targetIds` of client session ids. **`null` — the default — broadcasts to ALL connected clients.** In a multi-user app this is a privacy trap: an app instance is shared by every client connected to it, so a "reply" spoken without `targetIds` is heard by every user in the session, not just the one who asked.

```csharp
Audio.SpeechRecognizedAsync += async args =>
{
    // Reply only to the person who spoke — NOT the whole room.
    await Audio.SpeakAsync($"You said: {args.Text}", targetIds: [args.ClientSessionId]);
};
```

Also note: **interruption is instance-global, not per-target.** All speech flows through one mixer, so a new `SpeakAsync` fades out whatever is currently playing even when the two utterances target different clients. If two users must be spoken to independently at the same time, one shared `Audio.SpeakAsync` cannot do it — drive `SpeechGenerator` + `SendSpeech` with distinct chunk ids, or give each conversation its own mixing path.

### Stopping speech

`Audio.CloseAsync()` is **not** how you stop speech — it tears down an output stream, and the speech mixer's stream is the app's default output. Stop speech through the mixer:

```csharp
Audio.SpeechMixer.FadeOut();   // graceful: fade out the current utterance
Audio.SpeechMixer.Clear();     // hard reset: discard current, pending, and paused speech
```

`SpeechMixer` also offers `Pause()` / `Resume()`, and `WaitForCompletionAsync(speechEventId)` to await a specific utterance.

## Receiving audio from the microphone

Capture starts client-side. In the UI, `view.PushToTalkButton()` (hold to talk), `view.MicToggleButton()` (tap to open/close), or a `CaptureButton` starts a microphone stream; the server can also start one programmatically with `ClientFunctions.StartAudioCaptureAsync()`. Captured media always routes to the app on the server — other clients never receive the raw capture; the app decides any fan-out.

Pick one of the two mic buttons per microphone — offering both hold and toggle for the same mic is the ambiguity users report as "is it on?".

### The microphone permission is a separate press

Until the browser has granted a microphone, a capture button renders itself as an **"Enable microphone"** pill, and pressing it *only* asks for the permission — it never also starts a capture. Do not build a permission flow of your own around it.

The separation is what makes push-to-talk work at all. A permission dialog takes focus, and the page sees that as the button being released: a hold that doubles as the ask is cancelled behind the dialog, so the user grants access and finds that nothing was captured, on a button that now looks idle. After the grant the button flashes a green **ready** ring for two seconds, so "is it on now?" is answered before it is asked, and the next press is unambiguously a talk press.

A refusal (or a machine with no microphone) switches the button to a **"Microphone blocked"** state that stays pressable so it can explain itself, and fires `onPermissionChanged`:

```csharp
view.PushToTalkButton(
    text: "Hold to talk",
    onPermissionChanged: async args =>
    {
        _micBlocked.Value = args.State != MediaPermissionState.Granted;
    });
```

Handle it — offer typing instead, or point at the browser's site settings. And never gate a mic button behind `disabled:` for permission reasons: a disabled button cannot ask, so the user has no way out of the state. `disabled:` means "the app is busy".

Every state is stamped on the client as `data-ikon-capture-state` (`idle`, `pressed`, `live`, `ready`, `prompt`, `requesting`, `denied`, `unavailable`), so the feedback lands in the frame of the press rather than a server round trip later — `pressed` fires before the microphone has even finished opening. `Theming.MicButton.Default` renders all of them; a custom style array replaces it, so include `MicButton.States` (or lead with `"default"`) to keep them. Mirroring capture state into a `ClientReactive<bool>` from `onCaptureStart` is the wrong way round and is visibly late.

Flutter frontends run the same state machine against the OS permission dialog, so a mic button means the same thing in a browser and on a phone.

For transcription, prefer `UseSpeechRecognition` (next section). For raw PCM access:

```csharp
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
```

The event args carry `args.ClientSessionId` / `args.UserId` / `args.ClientContext` directly — never plumb client identity through a button's `onCaptureStart` into the frame handlers.

## Speech recognition

One call during app setup wires capture → transcription → routing:

```csharp
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
```

Exactly one of `SpeechRecognizedAsync` / `SpeechNotRecognizedAsync` fires per completed segment. If you latch busy state when capture stops (a "Transcribing..." spinner, a disabled button), release it in **both** handlers — handling only the success event leaves the spinner stuck for any press that produced no speech.

`SpeechRecognizedAsync` never fires unless `UseSpeechRecognition` (or `UseTurnDetection`) was called once at setup. Calling either twice, or both, throws `InvalidOperationException`.

### The `requireCorrelatedStream` flag

`UseSpeechRecognition(model, silenceThresholdRms: 0.01f, requireCorrelatedStream: true, language: "", timeout: null)`

`requireCorrelatedStream` defaults to **true**: recognition fires only for streams started by Parallax capture buttons (`PushToTalkButton`, `MicToggleButton`, `CaptureButton`), which stamp a `CorrelationId` on the stream. A capture started programmatically with `ClientFunctions.StartAudioCaptureAsync` has no correlation id and is **silently ignored** — the classic symptom is "the mic streams but `SpeechRecognizedAsync` never fires". Pass `requireCorrelatedStream: false` to transcribe every audio stream, including ad-hoc ones. `UseTurnDetection` has the same flag with the same default.

## Turn detection (open-mic conversations)

For an always-listening voice app, `UseTurnDetection` segments a continuous stream into conversational turns instead of transcribing per button press:

```csharp
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
```

Notable parameters: `speculative` (default true) starts transcription at the probable turn end so the confirmed turn adds zero recognition latency; `pauseWhileAppSpeaking` (default true) suppresses detection while the app is audibly speaking so its own voice can't trigger turns — set false for barge-in apps; `config` accepts a `TurnDetectorConfig` for silence windows, minimum speech length, or a plug-in VAD classifier.

## AudioChunk: construction rules

When feeding your own audio into `SendSpeech` (or a `SpeechMixer`), **always use the full constructor**:

```csharp
var chunk = new AudioChunk(
    id: Guid.NewGuid().ToString(),   // one unique id per utterance
    samples: samples,                 // float[] PCM in [-1, 1]
    sampleRate: 48000,
    channelCount: 1,
    isFirst: true,
    isLast: true);
Audio.SendSpeech(chunk);
```

Two traps:

- The parameterless constructor exists only for the serializer. An object initializer that skips fields leaves `SampleRate` and `ChannelCount` at `0`, and `SendSpeech` throws `ArgumentException` synchronously for such a chunk — inside whatever handler called it, so an unguarded call takes the handler down.
- The `Id` identifies the *speech event*. Chunks sharing an id are appended to one utterance; a **new** id interrupts the current utterance with a fade. Reusing the id of the utterance that just finished makes later chunks read as its tail and they are silently dropped; reusing an older id starts a new utterance that interrupts whatever is playing. One utterance, one unique id; a multi-chunk stream (e.g. streaming TTS) shares the id across its chunks with `isFirst`/`isLast` bracketing it.

## Group audio: calls and huddles

For meetings, huddles, and multiplayer voice, `GroupAudioMixer` (from `Ikon.Resonance`) mixes every participant's microphone into a personalized output per participant — each hears everyone **except themselves**:

```csharp
private readonly GroupAudioMixer _mixer = new();

// Wire participants and streams:
app.OnClientJoined(async ctx => _mixer.AddParticipant(ctx.ClientSessionId));
app.OnClientLeft(async ctx => _mixer.RemoveParticipant(ctx.ClientSessionId));

// The frame event carries only StreamId/Samples/IsFirst/IsLast — the format lives on the
// BEGIN event, so stash it per stream:
private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamFormats = new();

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
// are already real-time paced, so SendImmediateAsync is correct here:
_ = Task.Run(async () =>
{
    await foreach (var (participantId, frame) in _mixer.StreamAsync(ct))
    {
        await Audio.SendImmediateAsync(frame.Samples, frame.SampleRate, frame.ChannelCount,
            frame.IsFirst, frame.IsLast, frame.StreamId, targetIds: [participantId]);
    }
});
```

Rules that bite:

- **Every stream has an owning participant** (`AddStream(streamId, participantId)`), and that participant never hears that stream back — that is how echo of your own voice is excluded.
- **`WriteSamples` for an unregistered stream id is silently dropped** (with a throttled warning). Call `AddStream` from `AudioInputStreamBeginAsync` before any frame is written; forget it and that participant is inaudible with no error.
- Participants must be registered with `AddParticipant` to receive output; a participant with no streams of their own still hears everyone else. A lone speaker receives no frames (their mix would contain only themselves).
- The pump is single-consumer, and yielded frames alias one reused buffer — consume each frame inside the loop body; copy the samples if you keep them longer. Wrap the loop in a catch-and-restart so one bad frame can't silence the whole room. Get the sample rate and channel count from the stream's begin event; the mixer resamples to its native 48 kHz stereo internally.

## Video

Video is input-driven: clients capture camera or screen (a `CaptureButton`, or `ClientFunctions.StartVideoCaptureAsync`), the app receives the stream, and decides any fan-out. Render an outgoing stream on clients with `view.VideoStreamCanvas(streamId: ...)`.

```csharp
// The frame event carries no codec or geometry — those arrive once on the BEGIN event,
// so stash them per stream:
private readonly Dictionary<string, VideoInputStreamBeginEventArgs> _videoStreams = new();

Video.VideoInputStreamBeginAsync += async args => _videoStreams[args.StreamId] = args;

Video.VideoInputFrameAsync += async args =>
{
    // args.Data is ENCODED codec bitstream (see the codec on the begin event), not pixels.
    // Forward it as-is — e.g. echo to everyone except the sender:
    var stream = _videoStreams[args.StreamId];
    var targets = app.Clients.Ids.Where(id => id != args.ClientSessionId).ToList();
    await Video.SendFrameAsync(args.Data, args.FrameNumber, args.IsKey,
        args.TimestampInUs, args.DurationInUs, stream.Codec, stream.Width, stream.Height,
        stream.Framerate, streamId: args.StreamId, targetIds: targets);
};

Video.VideoInputStreamEndAsync += async args =>
{
    _videoStreams.Remove(args.StreamId);
    await Video.CloseAsync(args.StreamId);
};
```

Two hard rules for `SendFrameAsync`:

- **`data` must be an encoded bitstream matching the `codec` argument** (`VideoCodec.H264`, `Vp8`, `Vp9`, `Av1`). Never raw pixels, and never JPEG/PNG bytes — clients feed the data straight to a video decoder, and anything else produces a black or broken canvas, not an error. The only data most apps ever pass is what arrived in `VideoInputFrameAsync.Data`, forwarded unchanged. (For a still image, use `view.Image`, not a video stream.)
- **Frames are transmitted immediately — the caller owns the pacing.** Call once per frame at the source framerate, typically by forwarding each incoming frame as it arrives. Never loop over a stored clip's frames without pacing.

`Video.GetOutputStreamInfo(streamId)` describes an active output stream; `CloseAsync` / `CloseAllAsync` end streams. `targetIds: null` broadcasts to all clients, with the same multi-user caveat as audio.

## Telephony

Phone calls and SMS — including speaking and listening on a live call via `app.Telephony` and `IVoiceCall` — are a separate surface with their own guide: see `ikon-app-telephony-guide.md`. Telephony audio is G.711 mu-law at 8 kHz on the wire; the platform converts to and from the float PCM used everywhere in this guide.
