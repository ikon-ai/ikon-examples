<!-- checked-against: 81f4e8cfb606357895d7c18b -->

# Ikon Audio & Video Guide

How an Ikon AI app's C# app class plays audio to clients, receives microphone and camera streams, transcribes speech, and mixes group calls. Read this if your app makes sound, listens, or handles video.

## Setup: construct the services in a field initializer

`Audio` and `Video` are app services. Declare each as a property initialized from the app parameter of your app class's primary constructor:

On the app class:

<!-- ikon-example: av-accessors -->
```csharp
private Audio Audio { get; } = new(app);
private Video Video { get; } = new(app);
```

Construct each service once. The constructor subscribes to the app's incoming media messages, so a service constructed later than setup misses every stream that began before it, and a second instance decodes every incoming stream a second time. Playback needs no start-up step: the service mixes and paces each client's audio from construction on.

All Ikon namespaces are auto-imported through the app scaffold's `GlobalUsings.cs`, so no `using` directives are needed for any type in this guide.

## Playing audio

<!-- ikon-example: av-send -->
```csharp
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
```

Every sound is a **playback**, and each client hears every playback aimed at it, mixed. There are no streams to open, name or close; each listener hears at most 64 playbacks at once (see `Evicted` below). Three calls start one, and each returns an `AudioPlayback` at once — ignoring it is fine:

- **`Audio.Play`** plays a clip: an `AudioClip` (`AudioClip.FromWav` reads 8–32-bit integer or float WAV bytes) or interleaved `float[]` samples, copied before the call returns. Mono or stereo at any sample rate; any other channel count throws `ArgumentOutOfRangeException`, so downmix first. `PlayOptions.Loop` repeats it and `PlayOptions.Rate` changes speed and pitch together.
- **`Audio.PlayLive`** opens a `LiveAudioPlayback` the app writes into as it produces audio — a synth, a relayed realtime model, streamed TTS chunks. `WriteAsync` copies the samples and returns once they are queued, waiting while the buffer is `maxBufferAhead` ahead of playout (default 200 ms; a bursty relay wants about 2 s). It returns `false`, discarding the samples, once the playback has ended, so `while (await live.WriteAsync(buffer))` is the write loop. Silence is not an ending: with nothing written it stays open and plays nothing. `Clear()` drops what is buffered and keeps it open (a user barged in on a relay); `Complete()` plays out the buffer and ends it `Finished`; disposing it stops it with a fade.
- **`Audio.Speak`** generates speech and plays it. `SpeechOptions` picks `Model` (default `SpeechGeneratorModel.ElevenFlash25`), `Voice`, `Instructions` and `Speed` — ElevenLabs models fail the playback for `Instructions` (except Eleven3) and for a speed other than 1.0. Generation starts when the playback is next in its slot, and is cancelled when it has ended for every listener. To generate speech *without* playing it, use the one-shot `await SpeechGenerator.GenerateAsync(text)`, which returns a PCM `AudioChunk`.

`await playback.Completion` waits for playout and yields an `AudioPlaybackOutcome`; it never throws. `Finished` means it played to its end; `Stopped`, `Replaced` and `Evicted` (a listener already heard 64 playbacks: the oldest one-shot clip is cut to make room, or the new playback when nothing but loops, live playbacks, file and URL playbacks and speech is playing) mean something else ended it; `NoListeners` means nobody in its audience was connected (every connected client is a listener — a browser tab, an SDK client or an `ikon browse` session alike — except the app's own internal session); `Failed` means producing the audio failed, with `AudioPlayback.Error` saying why; `AppStopping` ends everything at app stop. When listeners ended it differently, `Finished` wins if anyone heard it to the end. `Started` completes when its timeline begins — at once, at the front of a queue, or on resume of a paused slot.

The handle acts for every listener: `Volume` (smoothed over 20 ms), `Pan` (-1 left, 0 centre, 1 right) and `Rate` (clips and cached sounds only — it throws on live, file and URL playbacks and speech) change as it plays, `FadeTo` ramps the volume, and `Pause`, `Resume` and `Stop` act on this playback alone. A playback has one timeline shared by everyone who hears it; `Position`, `IsPlaying`, `IsPaused` and `IsEnded` read it.

### Slots and mix modes

A **slot** is a plain string naming a group of playbacks; nothing is predefined. How a new playback treats those already playing in its slot is its `PlayOptions.Mode`, an `AudioMixMode`:

- `Mix` — plays alongside everything: effects, drum hits, overlapping clips.
- `Replace` — crossfades: the slot's current playbacks leave with their `FadeOut` while the new one enters with its `FadeIn`. A choke group, a music track change, a new line of speech.
- `Queue` — starts once everything earlier in the slot has finished. A queue behind a loop ends the loop at the end of its current pass; behind an open live playback it waits until that completes.

`Mode`, `Slot`, `FadeIn` and `FadeOut` left null take the defaults of the call:

| | `Mode` | `Slot` | `FadeIn` | `FadeOut` |
|---|---|---|---|---|
| `Play` | `Mix` | `"default"` | 2 ms | 10 ms |
| `PlayLive` | `Replace` | `"live"` | 20 ms | 150 ms |
| `Speak` | `Replace` | `"speech"` | 50 ms | 150 ms |

So `Speak(targets, text, options: new PlayOptions { Pan = 0.5f })` still replaces in `"speech"`. `Play` with `Replace` or `Queue` and no `Slot` throws `ArgumentException`, because `"default"` holds every unslotted sound. Two live sources at once each need their own slot or `Mode = AudioMixMode.Mix` — left in `"live"`, the second replaces the first. Speech audio you play yourself (a `SpeechGenerator` clip, a recorded line) belongs in `Slot = "speech"` with `Replace`: it then replaces earlier speech and counts as the app speaking for turn detection.

Replace and Queue consider only the playbacks in the slot that the new playback's listeners still hear, which is what makes targeted speech work (below). `PlayOptions.Duck` lowers other slots for each listener while the playback is heard, and they come back by themselves: `Duck = [new SlotDuck("music", 0.15f)]` dips the music to 15 % under a line (200 ms down and 800 ms up by default). Several ducks on one slot apply the lowest; a live playback ducks while audio written to it is playing.

`PlayOptions.Effects` and `PlayOptions.Analyzers` process one playback's audio, analyzers first; each playback creates its own effect state, so one list can be passed to many playbacks. Pass the same `VisemeAnalyzer` instance to every playback: a client learns the analysis shapes of its stream from the frame that first declares them, and a set it has not seen reopens that client's stream to declare it. Each value is tagged with the playback and slot it came from, so a frontend follows one speaker with `ikonClient.viseme.getCurrentVisemeValues({ slot: 'aria' })` (or `{ playbackId }`, the C# `AudioPlayback.Id`); with no filter it returns the most recently started playback that is speaking.

**Files and URLs.** `Audio.Play(targets, uri, options)` fetches a WAV, MP3 or Ogg (Vorbis or Opus) file (http or https, public addresses only, up to 100 MB) and decodes it a few seconds ahead of playout — the way to play music and long recordings without holding them decoded. `Audio.Play(targets, stream, mimeType, options)` does the same for a stream the app already has, and disposes it once read. Both take the clip options except `Rate`, loop by decoding the file again, are never cut to make room for another playback (though the new one can itself be evicted), and complete `Failed` with `AudioPlayback.Error` set when the file cannot be fetched or decoded. `AudioClip.DecodeAsync(bytes, mimeType)` decodes a short file completely into a clip — the format is read from the bytes, the MIME type only breaks ties.

**Cached sounds.** A short fixed sound replayed often — a pad hit, a click, a notification — is made once with `Audio.CreateSound(clip)` (at most 30 s and 5 MB as 16-bit WAV) and played with `Audio.Play(targets, sound, options)`, like a clip: same defaults, slots, modes and ducks. A client that can cache sounds receives the bytes once and plays its own copy, without the server's streaming delay; any other client hears it mixed. `AudioSound.PreloadAsync(targets)` puts it in the clients' caches before the first press — with `Everyone`, clients that join later too. A browser that has not had a user gesture yet cannot play: a one-shot ends `NoListeners` for that client and a loop starts at its live position on the first gesture. Effects and analyzers are refused for a cached sound (`Play` throws `ArgumentException`), and cached sounds do not count toward the 64-playback cap.

`Audio.Raw`, a `RawAudioOutput`, is the escape hatch: your own frames on your own stream id, sent at once — no mixing, no pacing, no slots, and `Stop` does not touch them. The caller paces them to real time, marks segments with `isFirst`/`isLast`, and frees the stream with `CloseStreamAsync`; each stream id holds client resources until then or app stop. `Audio.Raw.GetStreamInfo`, `Audio.Raw.GetPlaybackStatus` and `Audio.Raw.PlaybackReportReceivedAsync` report on raw streams only. Use it for audio already produced in real time, or an engine that mixes and paces itself; everything else is `Play`.

### `MediaTargets`: every send names its audience

Every send method takes a `MediaTargets` as its first argument, and there is no default — the compiler makes each call site say who it reaches. `MediaTargets.Everyone` broadcasts to all connected clients; `MediaTargets.To(...)` names client session ids.

State it deliberately. An app instance is shared by every client connected to it, so a "reply" sent to `Everyone` is heard by every user in the session, not just the one who asked.

A targeted send whose id list is **empty** transmits nothing at all. An empty target list is indistinguishable on the wire from no targets, which the server routes to every client, so a filter that matched nobody would otherwise reach exactly the clients it excluded.

The reply below assumes recognition is on: `Audio.SpeechRecognizedAsync` never fires until `Audio.UseSpeechRecognition` or `Audio.UseTurnDetection` has been called once at setup.

**Speech with nobody to hear it is not generated.** `Audio.Speak` decides its audience when its generation would start: `Everyone` with no client connected, or a target list naming only clients that have left, completes `NoListeners` and the speech models are never called. A playback to specific clients none of whom is connected completes `NoListeners` too, and a raw send to nobody transmits nothing. A playback to `Everyone` other than speech keeps its timeline in an empty room, so music started in `Main` reaches the clients who join later. This matters for an app that keeps working while its tab is closed: without it, an instance left running narrates to an empty room and is billed per character for it.

<!-- ikon-example: av-reply-to-speaker -->
```csharp
Audio.SpeechRecognizedAsync += async args =>
{
    // Reply only to the person who spoke — NOT the whole room.
    Audio.Speak(MediaTargets.To([args.ClientSessionId]), $"You said: {args.Text}");
};
```

`Replace` acts on what the new playback's listeners hear, so replying to each speaker like this lets two users be spoken to at the same time: a reply to one does not cut off the reply to the other. A reply to `MediaTargets.Everyone` replaces both.

### Stopping, pausing and volume

`Audio.Stop`, `Audio.Pause`, `Audio.Resume` and `Audio.SetSlotVolume` act on what the targeted clients hear in a slot (a null slot is every slot):

<!-- ikon-example: av-mixer-control -->
```csharp
Audio.Stop(MediaTargets.Everyone, "speech");                 // graceful: fade out all speech
Audio.Stop(MediaTargets.To(7), "speech", fade: false);       // hard stop: silence speech for client 7 only
Audio.Pause(MediaTargets.Everyone, "speech");                // hold speech where it is ...
Audio.Resume(MediaTargets.Everyone, "speech");               // ... and carry on
Audio.SetSlotVolume(MediaTargets.Everyone, "music", 0.3f);   // lower one slot, leave the rest
Audio.Stop(MediaTargets.Everyone);                           // every slot, everyone
```

With `Everyone` they act for every client, later joiners included: `Stop` ends the slot's playbacks and clears its queue, and `Pause` holds the timelines — a one-shot clip that arrives in a paused slot is dropped as `Stopped` instead of bursting out on resume. With specific clients they act for those clients alone: `Stop(MediaTargets.To(id), "speech")` is barge-in, and an `Everyone` playback plays on for everyone else (speech, playbacks to specific clients and one-shot clips left with no listener end). A per-client `Pause` mutes the slot for them while the timeline runs on, so they rejoin at the live position. Settings made with `Everyone` and with specific clients are separate layers: a client hears a slot at the shared volume × its own volume, and a host turning the music down for everyone keeps each guest's own setting. To continue after a line has played, `await playback.Completion`.

## Receiving audio from the microphone

Capture starts client-side. In the UI, `view.PushToTalkButton()` (hold to talk), `view.MicToggleButton()` (tap to open/close), or a `CaptureButton` starts a microphone stream; the server can also start one programmatically with `ClientFunctions.StartAudioCaptureAsync()`. Captured media always routes to the app on the server — other clients never receive the raw capture; the app decides any fan-out.

Pick one of the two mic buttons per microphone — offering both hold and toggle for the same mic is the ambiguity users report as "is it on?".

### The microphone permission is a separate press

Until the browser has granted a microphone, a capture button renders itself as an **"Enable mic"** pill, and pressing it *only* asks for the permission — it never also starts a capture. Do not build a permission flow of your own around it.

The separation is what makes push-to-talk work at all. A permission dialog takes focus, and the page sees that as the button being released: a hold that doubles as the ask is cancelled behind the dialog, so the user grants access and finds that nothing was captured, on a button that now looks idle. After the grant the button flashes a green **ready** ring for two seconds, so "is it on now?" is answered before it is asked, and the next press is unambiguously a talk press.

A refusal (or a machine with no microphone) switches the button to a **"Mic blocked"** state that stays pressable: a press asks again, which succeeds once the site settings allow it, and fires `onPermissionChanged`. The event fires only when a press asks: a microphone already blocked or missing when the page loads shows the state silently, and the handler first runs on the first press:

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
    // args.Reason: NoAudio, Silence, NoSignal (a muted or virtual mic: tell the user to check which mic their device uses), NoText, or Error (failure in args.Error).
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

When building an `AudioChunk` yourself, **always use the full constructor**:

<!-- ikon-example: av-audio-chunk -->
```csharp
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
```

Two traps:

- There is no public parameterless constructor, so `new AudioChunk { ... }` does not compile. `Audio.Play` and `LiveAudioPlayback.WriteAsync` take only mono or stereo and throw `ArgumentOutOfRangeException` for any other channel count — `Play` synchronously, `WriteAsync` when its task is awaited (`PlayLive` itself accepts the count) — downmix wider audio first — inside whatever handler called them, so an unguarded call takes the handler down.
- The `Id` names one utterance: a streaming generator gives every chunk of an utterance the same id, bracketed by `isFirst`/`isLast`. Playing the chunks of one utterance means writing them, in order, into one `Audio.PlayLive` playback; a new utterance is a new playback, whose default `Replace` crossfades the previous one in its slot.

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
// are already mixed and real-time paced, so the raw lane is correct here:
_ = Task.Run(async () =>
{
    await foreach (var (participantId, frame) in _mixer.StreamAsync(ct))
    {
        await Audio.Raw.SendFrameAsync(MediaTargets.To([participantId]), frame.StreamId, frame.Samples, frame.SampleRate,
            frame.ChannelCount, frame.IsFirst, frame.IsLast);
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
