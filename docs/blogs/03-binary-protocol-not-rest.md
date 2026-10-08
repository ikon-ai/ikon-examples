# Binary Protocol, Not REST

*Published 2026-03-19*

Every modern web application uses the same approach: JSON over HTTP. The client makes a request, the server sends a response, and the connection closes. Real-time updates need WebSockets, file uploads and streaming each need their own mechanism, and video needs a separate media server. Each requirement adds another layer on top of the original request-response model.

In Ikon, a single persistent connection carries interface updates, audio streams, video frames, function calls, and events, all multiplexed over one channel. There are no REST endpoints, no GraphQL, and no separate real-time servers.

## What you can build

Because everything goes over one connection, you can build some kinds of AI applications without specialized infrastructure knowledge.

**Build voice and video AI apps without media expertise.** Building a voice-enabled AI application on a traditional stack means setting up a media server, coordinating signaling protocols, handling codec negotiation, and somehow synchronizing audio with your AI responses. On Ikon, audio frames travel over the same connection as interface updates and AI responses. A developer built a full video conferencing app with per-participant speech recognition and real-time AI summaries. It uses one connection per client and no media server or signaling protocol. The audio, the transcription updates, and the AI-generated summaries all arrive interleaved on the same channel.

**Multimodal apps over a single connection.** Consider an app where people describe a scene in natural language, the AI generates a visualization, and synthesized speech narrates the result. That is three modalities (text, graphics, and audio) going in both directions. On a traditional stack, you'd coordinate a separate service for each. On Ikon, it's one connection with different message types. The creator doesn't have to think about transport and can focus on what the AI should do.

**Interactions that feel instant.** In interactive AI applications, people type and expect streaming responses, speak and expect real-time transcription, and click and expect immediate feedback. The overhead of traditional approaches adds up across all these interactions. On Ikon, no interaction pays for per-request framing, headers, or text-based parsing. For an AI character with lip-synced speech and reactive expressions, that overhead decides whether the character feels alive or seems to be buffering.

**The server can contact the client without being asked.** The server can push interface updates, stream audio, and invoke client-side functions without a request from the client. An AI agent can work in the background and push results when they are ready. Text and speech can stream in sync. These are common patterns in interactive AI applications, and the connection supports them directly.

## Teleport: a protocol built for real-time apps

Ikon's communication layer is built on **Teleport**, a binary format designed for real-time interactive applications. Every message has a 27-byte header followed by a payload. In the payload, field names are replaced with short identifiers at build time.

As a result, a typical interface update that would be 2KB in JSON is a few hundred bytes in Teleport, before compression is applied.

## One connection for every channel

Connection management, heartbeats, application events, function calls, interface updates, audio, and video all go over a single persistent connection. When someone speaks into their microphone, the audio arrives on the same connection that carries their interface updates. When the server generates a response with text, tool calls, and a synthesized voice, all of it streams back on the same connection.

You don't have to coordinate several kinds of connection, and you never end up with the real-time connection down while the API still works. There is one connection, with one reconnection strategy and one keepalive mechanism.

## Connection handling

The connection layer supports several transports and picks one automatically. The client tries the fastest transport first, falls back to another if it fails, and remembers which one worked so the next reconnection is faster.

After a brief disconnection (under five minutes), the client reconnects quickly without authenticating again. After a longer gap, it does a full reconnect. The creator doesn't need to write code for either case.

## How this feels in practice

With a persistent connection, the user does something, the message is written to the existing connection, the server processes it, and the response streams back. No exchange needs connection setup, header overhead, or text-based parsing.

In interactive AI applications, people expect immediate feedback when they type, speak, or click, and they notice the difference.

## Two-way communication

Traditional web architecture is one-directional: the client asks and the server answers. For the server to push data to the client, you have to add a separate mechanism.

Ikon's protocol is two-way from the start. The server can push interface updates, stream audio, and invoke functions on the client without the client asking. The client can send input, stream audio, and call server functions. Both directions use the same protocol and the same message format.

This makes several patterns possible that are awkward or impossible with traditional approaches:

**Interleaved streaming**: An AI generates text and speech audio at the same time. Both streams arrive interleaved on the same connection, and the client shows the text and plays the audio in sync.

**Live interface without polling**: When a background AI task completes, the server updates the interface and pushes the change. The client gets the update as soon as it is ready, without polling.

**Server-initiated queries**: The server can ask the client for information, such as GPS coordinates, camera access, or local data, and get a response over the same connection.

## Built for production

Production systems have to cope with slow clients, network congestion, and bursts of traffic. The protocol layer uses bounded message queues, separate backpressure for each channel, and a connection limit per server instance. If a client can't keep up, the server drops its connection instead of letting memory grow without limit. Unbounded memory growth of this kind has caused production outages at companies with dedicated infrastructure teams.

REST is still a good fit for simple, stateless interactions. Interactive AI applications keep state, stream data in both directions, mix several media types, and need low latency. The assumptions behind traditional web communication work against these applications, and Teleport was designed for them.
