# Video Conferencing with AI in a Few Thousand Lines

*Published 2026-03-19*

You join a meeting. When someone starts talking, their name lights up at once and stays lit through short pauses, instead of flickering with every breath. Under the video, a live transcript shows each participant's words in real time, labelled with the right speaker. In a side panel, an AI-generated summary of the key points is updated as the meeting goes on. If you join late, you can read the summary and catch up in seconds.

This app has video conferencing, live transcription and AI meeting summaries. One person built it in under four thousand lines of code.

## Why one person could build it

**Multiuser without extra code.** Nobody wrote multiuser support for this app. No code says "when participant A's transcript updates, send it to participants B, C, and D", and there is no event routing, real-time sync layer or pub/sub configuration. The developer updates a value, and every connected participant's screen shows the change, because on Ikon state is shared with every client by default.

**One developer instead of a team.** Video conferencing with AI features is one of the hardest kinds of application to build. On a traditional stack, you need a WebRTC signaling server, a media routing server, a speech-to-text microservice, a separate AI service for summarization, a real-time state sync layer, and a frontend application with its own state management. That takes several teams several months. This app is one person's work in a few files.

**AI features built into the app.** Transcription and summarization run inside the same application process as the rest of the app, with direct access to the audio streams and shared state. They are not separate services connected with API calls and message queues.

## The experience in detail

### Audio routing

Every participant's microphone audio goes to every other participant but not back to the sender. On a traditional stack, this requires a selective forwarding unit that tracks which streams go where, plus ICE negotiation and TURN server fallbacks for network traversal.

On Ikon, the application states the rule: send each participant's audio to everyone except themselves. The platform handles the transport, so there is no signaling server to configure, no peer connections to manage and no media server to deploy.

### Speaker detection

When someone starts talking, their indicator lights up immediately. When they pause between sentences, it stays lit. When they stop talking, it fades. This is harder to get right than it sounds.

The app tracks each speaker's volume with a moving average that rises quickly when someone starts speaking and falls slowly during a pause between words. So the indicator turns on at once but does not flicker during normal speech. If someone mutes or disconnects, no audio arrives, and after a couple of seconds a timeout resets the indicator.

The entire speaker detection logic is two lines:

```csharp
float alpha = rmsVolume > state.EmaVolume ? EmaAlphaUp : EmaAlphaDown;
state.EmaVolume = (alpha * rmsVolume) + ((1 - alpha) * state.EmaVolume);
```

`EmaAlphaUp` is 0.4, so the average rises quickly when someone starts speaking. `EmaAlphaDown` is 0.03, so it falls slowly and brief pauses don't turn the indicator off.

Details like this make an app feel finished instead of like a prototype. The developer had time for them because the platform handled so much else.

### Live transcription for every participant

Each participant has their own speech recognizer. Their audio first goes through a silence filter, which removes silence before it reaches the model, and is then split into segments at pauses. The recognized text is added to a shared transcript, and every participant sees it update in real time.

Adding an entry to the shared transcript is all it takes to send it to everyone. The developer writes a new entry to the transcript list, and every connected participant's screen updates to show it. There is no event bus, WebSocket broadcast code or frontend subscription logic.

### AI summaries that build incrementally

A meeting that runs for an hour generates a lot of transcript. Sending the entire history to an AI model every time you want an updated summary would be wasteful and slow.

Instead, the app tracks what has already been summarized. Every 60 seconds, it checks whether new transcript entries or chat messages have arrived since the last summary update. If so, it sends only the new content to the AI along with the previous summary, asking it to add the new information. As the meeting goes on and the summary grows, the AI is told to drop less important details, so the summary stays useful and does not grow without limit.

## Three scopes of state

The app uses three scopes of state, and each is handled differently:

**State everyone sees together.** The participant list, transcript entries, chat messages, and the AI-generated summary. When any of these change, every participant's screen updates. The developer declares these as shared values.

**State personal to each connection.** Whether your camera is on, which settings tab you have open, your chat input text, your device selections. Your camera toggle does not affect anyone else's camera state. The developer declares these as per-client values.

**State that follows a user across devices.** Theme preference, timezone, device type. If the same person connects from a laptop and a phone, both sessions pick up their preferences.

In a traditional stack, implementing these three scopes of state means building separate state management layers: shared stores with selective broadcasting, session-scoped state with connection affinity, and a user preferences database with cross-device sync. Here, the developer chooses the type of value, and the framework handles the rest.

## Multiuser without broadcast code

The app only chooses who receives what where the choice matters to the meeting. Audio is sent to everyone except the sender, to prevent echo, and video is sent to everyone including the sender, for self-view.

Everything else (transcripts, summaries, chat messages, the participant list and speaking indicators) is declared as shared state, so it reaches everyone automatically. The developer never writes broadcast logic or configures channels. They update a value, and the platform makes sure everyone sees it.

## What this demonstrates

A video conferencing app tests a platform hard. It needs real-time media routing, several participants at once, live AI processing, three scopes of state, and a layout that works on desktop and mobile. It uses audio, video, speech recognition, AI orchestration, and reactive UI in one application.

The app also has configurable speech-to-text models, configurable AI models for summarization, device selection, screen sharing, theme switching, mobile layout detection, and meeting link generation. All of it fits in under four thousand lines because the platform handles transport, rendering, and real-time sync. The developer only makes the decisions specific to this app: how audio is routed, how speech is segmented, and how summaries are structured.
