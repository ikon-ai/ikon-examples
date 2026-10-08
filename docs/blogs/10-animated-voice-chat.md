# Animated Voice Chat with Live2D Characters

*Published 2026-03-19*

Talk to an animated character, and it answers with lip-synced speech. Its mouth shapes match each syllable, and its expression changes with the conversation. If you interrupt it mid-sentence, the character's voice fades out and it stops talking. You can stack audio effects on the voice, such as reverb, a robot filter or telephone crackle, and adjust them while the character speaks. There are five characters and three camera angles to choose from, and several people can join the same conversation at once.

The whole app is about twelve hundred lines in a single project. This post explains how it works.

## What you experience

You press a button and start talking. Your voice is captured, transcribed, and sent to an AI that generates a conversational response. That response is converted to speech, and the animated character delivers it with synchronized lip movements. Transcribing, replying, speaking and animating all run in a single process, so there are no hand-offs between separate services to add noticeable delays.

Five character models are available, each with its own personality in the animation. Three camera angles let you frame the conversation: full body, portrait, or close-up on the face. Each character needs its own framing settings at each zoom level, because every artist rigs their model differently. Settings that give a good portrait of one character are completely wrong for another.

## Multiuser by default

Multiple people can connect and talk to the same character, see each other's messages, and hear the same voice. When one person changes the character model or adds an audio effect, every connected user sees the change immediately.

The app has no code for this. The server keeps the shared state, and the platform sends every change to every connected client. So the app works for a group as it is: a team can talk to the character together, or a presenter can demonstrate the voice interaction while an audience watches on their own screens.

## Two ways to listen

The app supports two speech recognition modes, and they feel different to use.

**Batch mode** waits for you to finish speaking, then processes the entire recording at once. You press a button, say your piece, release, and the transcription appears. This mode is predictable and suits deliberate, turn-based conversation.

**Continuous mode** streams your speech to the recognizer as you talk, producing partial transcriptions in real time. You see your words appearing as you say them. For recognizers that do not support continuous input themselves, the app uses silence detection to split the audio, treating a half-second pause as the end of a sentence.

Microphone input arrives frame by frame, but the recognizer wants a continuous stream. A message queue collects the audio frames and passes them to the recognizer at the recognizer's own pace. When you stop talking, the queue closes and the recognizer finishes the audio it already has.

## Smooth interruption

If you start talking while the character is still speaking, the character's voice fades out instead of cutting off, the speech generation is cancelled, and your microphone turns on. Because the voice fades, there is no audio pop or awkward silence, and the turn passes straight to you.

The entire interruption handler is three lines:

```csharp
Audio.StopSpeech(MediaTargets.Everyone);
StopSpeaking();
_sttIsToggleRecording.Value = true;
```

The three lines fade the character's voice, cancel the speech generation and start listening. On a traditional stack, coordinating audio output, speech cancellation and microphone activation across several services would take significant engineering work.

## Orderly conversation

User messages do not go directly to the AI. They go through a queue that processes them one at a time, in order. When someone sends several messages quickly, this stops the AI responses from overlapping and the speech from playing on top of itself. Each message waits until the previous response has finished speaking.

## Lip sync that never drifts

The character's mouth movements come from viseme data, which says which mouth shape matches each moment of the audio. The viseme data is embedded in the audio stream itself, in the same packet as the audio it matches.

Because there is no separate channel for lip sync data and no timestamp matching between audio playback and mouth animation, the mouth stays in sync with the audio whatever the network conditions, buffering or latency.

The character is a Live2D model, a 2D illustration that moves and changes expression in real time using WebGL. The server controls which model is loaded, what expression is shown, and what motion is playing. The client handles the rendering.

## Eight chainable audio effects

The app includes eight audio effects that can be stacked and adjusted in real time: Delay, Reverb, Chorus, Tremolo, BitCrusher, Saturation, RobotVoice, and Telephone. Each has its own set of parameters.

The effects are real signal processing: delay lines with feedback and damping, Schroeder reverb, ring modulation for the robot voice, bandpass filtering for the telephone effect. You can layer Reverb on top of RobotVoice on top of Telephone and adjust each one's parameters while the character is speaking. Moving a slider changes how the next audio chunk sounds.

The effects are applied on the server before the audio reaches the client, so there is no processing cost on the user's device. On a traditional stack, this would be either an audio processing chain in the browser, with the complexity of the Web Audio API, or a separate audio processing service with its own protocol for parameter updates. In this app it is a list of effects passed to the audio output.

## What would normally be six-plus services

Consider what building this on a conventional web stack would require:

**Audio transport**: a protocol for bidirectional audio streaming, codec negotiation, jitter buffering, echo cancellation.

**Speech services**: separate speech-to-text and text-to-speech services, each with their own setup, authentication, and error handling. A message broker between them and the application.

**AI conversation**: an API client with streaming support, conversation history management, and retry logic.

**Character rendering**: a WebGL canvas with the Live2D SDK, a connection for receiving model parameters from the server, timestamp synchronization between audio playback and mouth animation.

**Audio processing**: a real-time audio effects pipeline with dynamic parameter updates.

**State management**: a store for conversation history, audio stream state, effect parameters, model selection, and view mode. Synchronization between server and client. Race condition management for concurrent audio streams and AI calls.

**Multiuser support**: additional channels, presence management, state synchronization between connected clients.

Each of those has to be integrated, debugged and maintained on its own. In this app, all of it is in the same twelve hundred lines.

## The actual scope

The app is twelve hundred lines. It has five selectable models, three camera angles, two speech recognition modes, smooth speech interruption, eight chainable audio effects with live parameter control, ordered message handling, viseme-driven lip sync and built-in multiuser support.

The code is short, but not because it hides the hard parts behind a thin wrapper. The audio effects are real signal processing. The speech recognition supports both batch and continuous modes with silence detection. The message queue handles concurrency correctly. The viseme data is embedded in audio frames so the lip sync does not drift.

The code is short because the platform does the integration work. The app has no glue code between the speech recognizer and the AI, no protocol for streaming audio parameters, no state synchronization layer between server and client, and no separate infrastructure for multiuser support. Each of those would be hundreds or thousands of lines on a conventional stack, and none of them is the interesting part of the application.

The interesting part is a character that listens to you and answers with lip-synced speech through a robot voice filter while someone else watches. That part is the twelve hundred lines you write.
