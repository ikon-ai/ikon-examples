# Ikon Audio & Video Guide
<!-- checked-against: b1ed6e7e5ade058f -->
How an Ikon AI app's C# app class plays audio to clients, receives microphone and camera streams, transcribes speech, and mixes group calls. Read this if your app makes sound, listens, or handles video.

## Setup: construct the services in a field initializer

`Audio` and `Video` are app services. Declare each as a property initialized from the app parameter of your app class's primary constructor:

On the app class:

<!-- ikon-example: av-accessors -->
```csharp
private Audio Audio { get; } = new(app);
private Video Video { get; } = new(app);
```

Construct each service once. The constructor subscribes to the app's incoming media messages, so a service constructed later than setup misses every stream that began before it, and a second instance decodes every incoming stream a second time. Speech needs no start-up step: each target set's speech mixer starts the loop that plays it out on the first utterance sent to that set.

All Ikon namespaces are auto-imported through the app scaffold's `GlobalUsings.cs`, so no `using` directives are needed for any type in this guide.

## Sending audio: two lanes, three methods

<!-- ikon-example: av-send -->
```csharp
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
```

`SpeakAsync` returns when the utterance is queued; `SpeakAndWaitAsync` completes when playout finishes (an interruption by a newer call completes it quietly). Both throw `TimeoutException` when the playout pipeline stops draining while unpaused, and `SpeakAndWaitAsync` throws `InvalidOperationException` when the mixer abandons the utterance — an utterance that never played is never reported as one that did. Both take optional `model` (default `SpeechGeneratorModel.ElevenFlash25`), `voice`, `instructions`, and `speed`. To generate speech *without* playing it, use the one-shot `await SpeechGenerator.GenerateAsync(text)`, which returns a PCM `AudioChunk`.

### The lane is in the name

`Speak*` goes through a **speech mixer per target set**: one utterance at a time for each distinct set of clients, each on its own output stream. Starting an utterance fades out the speech of every target set that shares a client with it, and only those — a `SpeakAsync` to client A leaves speech to client B playing, while one to `MediaTargets.Everyone` shares every client and so stops all speech, and a targeted one stops a broadcast that is playing.

`Send*` / `Play*` go straight to the wire as independent streams keyed by `streamId`, and overlap freely, including with speech.

So **two voices to the same listener at once is `PlayClipAsync` on two stream ids**, not two `SpeakChunk` calls — those interrupt each other, because a chunk carrying a new id supersedes what is playing to the same clients. `SpeakChunk` exists for generator settings `SpeakAsync` does not expose and for raw sample access, not for overlap.

Don't run two concurrent `PlayClipAsync` calls on the same stream id — the interleaved frames corrupt client playback. Use distinct stream ids or await the previous call first.

### `MediaTargets`: every send names its audience

Every send method takes a `MediaTargets` as its first argument, and there is no default — the compiler makes each call site say who it reaches. `MediaTargets.Everyone` broadcasts to all connected clients; `MediaTargets.To(...)` names client session ids.

State it deliberately. An app instance is shared by every client connected to it, so a "reply" sent to `Everyone` is heard by every user in the session, not just the one who asked.

A targeted send whose id list is **empty** transmits nothing at all. An empty target list is indistinguishable on the wire from no targets, which the server routes to every client, so a filter that matched nobody would otherwise reach exactly the clients it excluded.

The reply below assumes recognition is on: `Audio.SpeechRecognizedAsync` never fires until `Audio.UseSpeechRecognition` or `Audio.UseTurnDetection` has been called once at setup.

**An audio send with nobody to hear it does nothing, and `SpeakAsync` does not generate the speech.** `Everyone` with no client connected, or a target list naming only clients that have left, is an audience of nobody — every `Audio` send returns without transmitting, and the speech models are never called. The skip is logged at debug. This matters for an app that keeps working while its tab is closed: without it, an instance left running narrates to an empty room and is billed per character for it.

<!-- ikon-example: av-reply-to-speaker -->
```csharp
Audio.SpeechRecognizedAsync += async args =>
{
    // Reply only to the person who spoke — NOT the whole room.
    await Audio.SpeakAsync(MediaTargets.To([args.ClientSessionId]), $"You said: {args.Text}");
};
```

Because interruption follows the targets, replying to each speaker like this lets two users be spoken to at the same time: a reply to one does not cut off the reply to the other. A reply to `MediaTargets.Everyone` still interrupts both.

### Stopping speech

`Audio.CloseAsync()` is **not** how you stop speech — it tears down an output stream, and with no id it closes the `Play*`/`Send*` default stream, never a speech stream (`Audio.GetSpeechStreamId(targets)`). Stop speech by its targets; like a new utterance, a stop reaches every target set that shares a client with the targets given:

<!-- ikon-example: av-mixer-control -->
```csharp
Audio.StopSpeech(MediaTargets.Everyone);               // graceful: fade out all speech
Audio.StopSpeech(MediaTargets.To(7), fade: false);     // hard stop: discard speech that reaches client 7
```

`Audio.PauseSpeech(targets)` / `Audio.ResumeSpeech(targets)` hold and release playout the same way; await `SpeakAndWaitAsync` to continue after an utterance has played out.

## Receiving audio from the microphone

Capture starts client-side. In the UI, `view.PushToTalkButton()` (hold to talk), `view.MicToggleButton()` (tap to open/close), or a `CaptureButton` starts a microphone stream; the server can also start one programmatically with `ClientFunctions.StartAudioCaptureAsync()`. Captured media always routes to the app on the server — other clients never receive the raw capture; the app decides any fan-out.

Pick one of the two mic buttons per microphone — offering both hold and toggle for the same mic is the ambiguity users report as "is it on?".

### The microphone permission is a separate press

Until the browser has granted a microphone, a capture button renders itself as an **"Enable microphone"** pill, and pressing it *only* asks for the permission — it never also starts a capture. Do not build a permission flow of your own around it.

The separation is what makes push-to-talk work at all. A permission dialog takes focus, and the page sees that as the button being released: a hold that doubles as the ask is cancelled behind the dialog, so the user grants access and finds that nothing was captured, on a button that now looks idle. After the grant the button flashes a green **ready** ring for two seconds, so "is it on now?" is answered before it is asked, and the next press is unambiguously a talk press.

A refusal (or a machine with no microphone) switches the button to a **"Microphone blocked"** state that stays pressable so it can explain itself, and fires `onPermissionChanged`:

<!-- ikon-example: av-push-to-talk -->
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

<!-- ikon-example: av-audio-input -->
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

<!-- ikon-example: av-speech-recognition -->
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

`SpeechRecognizedEventArgs.Transcript` carries the full result — pass
`timestamps: SpeechTimestamps.Word` (or `Segment`) to `UseSpeechRecognition` / `UseTurnDetection` and
`args.Transcript.Words` is populated, with offsets relative to the start of the recognized segment
rather than of the stream. It defaults to `None`, and `args.Text` is unchanged either way. Not every
model can produce them: check `SpeechRecognizer.GetCapabilities(model).SupportsWordTimestamps` (or
`SupportsSegmentTimestamps`) first, because an unsupported granularity fails recognition of every
segment — `SpeechNotRecognizedAsync` fires with `Reason == Error` instead of `SpeechRecognizedAsync`.

Exactly one of `SpeechRecognizedAsync` / `SpeechNotRecognizedAsync` fires per completed segment, and
per detected turn: a turn that produced no transcript reaches `SpeechNotRecognizedAsync` with
`args.TurnId` naming it, rather than being dropped. If you latch busy state when capture stops (a "Transcribing..." spinner, a disabled button), release it in **both** handlers — handling only the success event leaves the spinner stuck for any press that produced no speech.

`SpeechRecognizedAsync` never fires unless `UseSpeechRecognition` (or `UseTurnDetection`) was called once at setup. Calling either twice, or both, throws `InvalidOperationException`.

### The `requireCorrelatedStream` flag

`UseSpeechRecognition(model, silenceThresholdRms: 0.01f, requireCorrelatedStream: true, language: "", timestamps: SpeechTimestamps.None, timeout: null)`

`requireCorrelatedStream` defaults to **true**: recognition fires only for streams that carry a `CorrelationId`. Parallax capture buttons (`PushToTalkButton`, `MicToggleButton`, `CaptureButton`) stamp one, and `ClientFunctions.StartAudioCaptureAsync` generates one when `ClientAudioCaptureOptions.CorrelationId` is null, so both are transcribed. A stream with no correlation id — one an SDK client sends on its own — is **silently ignored**; the classic symptom is "the mic streams but `SpeechRecognizedAsync` never fires". Pass `requireCorrelatedStream: false` to transcribe every audio stream, including ad-hoc ones. `UseTurnDetection` has the same flag with the same default.

## Turn detection (open-mic conversations)

For an always-listening voice app, `UseTurnDetection` segments a continuous stream into conversational turns instead of transcribing per button press:

<!-- ikon-example: av-turn-detection -->
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

This turn detector hears the audio and nothing else. Some recognizers judge the turn themselves,
from the words as well as the pause, and say so on `TranscriptEvent.IsEndOfTurn` when you drive
`RecognizeContinuousSpeechAsync` directly — `SpeechRecognizer.GetCapabilities(model).TurnDetection`
says which, and the Ikon.AI library overview covers the knobs. The two are separate mechanisms: this
one segments a stream for you, that one reports what the provider concluded.

## AudioChunk: construction rules

When feeding your own audio into `SpeakChunk` (or a `SpeechMixer`), **always use the full constructor**:

<!-- ikon-example: av-audio-chunk -->
```csharp
var chunk = new AudioChunk(
    id: Guid.NewGuid().ToString(),   // one unique id per utterance
    samples: samples,                 // float[] PCM in [-1, 1]
    sampleRate: 48000,
    channelCount: 1,
    isFirst: true,
    isLast: true);
Audio.SpeakChunk(MediaTargets.Everyone, chunk);
```

Two traps:

- There is no public parameterless constructor, so `new AudioChunk { ... }` does not compile. `SpeakChunk` throws `ArgumentException` synchronously for a `ChannelCount` other than 1 or 2 — the mixer takes only mono or stereo, so downmix wider audio first — inside whatever handler called it, so an unguarded call takes the handler down.
- The `Id` identifies the *speech event*. Chunks sharing an id are appended to one utterance; a **new** id interrupts the current utterance with a fade. A chunk carrying the id of the most recently completed utterance is dropped with a warning — unless it is marked `isFirst`, which starts a new utterance under that same id. Any other id, including that of an earlier completed utterance, starts a new utterance and interrupts what is playing. One utterance, one unique id; a multi-chunk stream (e.g. streaming TTS) shares the id across its chunks with `isFirst`/`isLast` bracketing it.

## Group audio: calls and huddles

For meetings, huddles, and multiplayer voice, `GroupAudioMixer` (from `Ikon.Resonance`) mixes every participant's microphone into a personalized output per participant — each hears everyone **except themselves**:

<!-- ikon-example: av-group-mixer-fields -->
```csharp
private readonly GroupAudioMixer _mixer = new();

// The frame event carries no SampleRate/ChannelCount — the format lives on the
// BEGIN event, so stash it per stream:
private readonly Dictionary<string, (int SampleRate, int ChannelCount)> _streamFormats = new();
```

Then from Main:

<!-- ikon-example: av-group-mixer -->
```csharp
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
```

Rules that bite:

- **Every stream has an owning participant** (`AddStream(streamId, participantId)`), and that participant never hears that stream back — that is how echo of your own voice is excluded.
- **`WriteSamples` for an unregistered stream id is silently dropped** (with a throttled warning). Call `AddStream` from `AudioInputStreamBeginAsync` before any frame is written; forget it and that participant is inaudible with no error.
- Participants must be registered with `AddParticipant` to receive output; a participant with no streams of their own still hears everyone else. A lone speaker receives no frames (their mix would contain only themselves).
- The pump is single-consumer, and yielded frames alias one reused buffer — consume each frame inside the loop body; copy the samples if you keep them longer. Wrap the loop in a catch-and-restart so one bad frame can't silence the whole room. Get the sample rate and channel count from the stream's begin event; the mixer resamples to its native 48 kHz stereo internally.

## Video

Video is input-driven: clients capture camera or screen (a `CaptureButton`, or `ClientFunctions.StartVideoCaptureAsync`), the app receives the stream, and decides any fan-out. Render an outgoing stream on clients with `view.VideoStreamCanvas(streamId: ...)`. The canvas takes an optional `onTap` handler called with `VideoTapArgs` — the tap position normalized to the rendered frame (0..1 on both axes), useful when the stream mirrors an interactive surface such as a device screen.

<!-- ikon-example: av-video-streams-field -->
```csharp
// The frame event carries no codec or geometry — those arrive once on the BEGIN event,
// so stash them per stream:
private readonly Dictionary<string, VideoInputStreamBeginEventArgs> _videoStreams = new();
```

Then from Main:

<!-- ikon-example: av-video-forward -->
```csharp
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
```

Two hard rules for `SendFrameAsync`:

- **`data` must be an encoded bitstream matching the `codec` argument** (`VideoCodec.H264`, `Vp8`, `Vp9`, `Av1`). Never raw pixels, and never JPEG/PNG bytes — clients feed the data straight to a video decoder, and anything else produces a black or broken canvas, not an error. The only data most apps ever pass is what arrived in `VideoInputFrameAsync.Data`, forwarded unchanged. (For a still image, use `view.Image`, not a video stream.)
- **Frames are transmitted immediately — the caller owns the pacing.** Call once per frame at the source framerate, typically by forwarding each incoming frame as it arrives. Never loop over a stored clip's frames without pacing.

`Video.GetOutputStreamInfo(streamId)` describes an active output stream; `CloseAsync` / `CloseAllAsync` end streams. `SendFrameAsync` takes the same `MediaTargets` first argument as audio, with the same multi-user caveat.

## Telephony

Phone calls and SMS — including speaking and listening on a live call via `app.Telephony` and `IVoiceCall` — are a separate surface with their own guide: see `app-telephony-guide.md`. Telephony audio on the wire is 16 kHz linear PCM on 46elks and G.711 mu-law at 8 kHz on Twilio; the platform converts to and from the float PCM used everywhere in this guide.
