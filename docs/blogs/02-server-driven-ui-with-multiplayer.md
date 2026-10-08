# Server-Driven UI with Built-In Multiplayer

*Published 2026-03-19*

Every web framework today assumes the same architecture: the server sends data and the client renders the interface. On top of that you need state management libraries, client-side routers, hydration strategies, and WebSocket layers for real-time updates. All of them exist to get data from the server, where it lives, to the client, where it is drawn on screen.

In Ikon, the interface is defined on the server and compared with its previous version on the server, and only the changes are streamed to each connected client. The client only draws the interface and has no business logic. Because the server controls what every connected person sees, multiuser collaboration needs no extra code.

## Multiuser without extra code

Ikon's server renders the interface for every connected client, so collaboration works without you adding anything.

A developer built a full video conferencing app with per-participant speech recognition, live transcription, and AI-generated meeting summaries, all updating in real time. Participants see each other's transcripts appear as they speak, watch AI summaries being written while the meeting is still going, and work with shared artifacts. The developer wrote no real-time synchronization code. Every client stayed in sync because the server renders the interface for all of them.

When AI generates content, such as an analysis, a visualization, or a piece of creative work, every connected person sees it appear as it is generated. In one data visualization app, people ask questions in natural language and the AI plots data points on a 3D globe. When one person asks a question, everyone watching sees the globe update.

## What you can build

Rendering on the server makes the technical stack simpler. It also means one person can build applications that previously required separate frontend, backend, and infrastructure teams.

**Build real-time collaborative AI apps without real-time expertise.** The video conferencing app above was built by one developer, who wrote no WebSocket code, no pub/sub configuration, and no client-side state synchronization. Ikon's reactive values handled all of it.

**One person can build what would otherwise take a team.** Consider what it takes to build an animated AI character with lip-synced speech, reactive facial expressions, and the ability to switch between different AI models mid-conversation. On a traditional stack, you'd need a frontend engineer for the character rendering, a backend engineer for the AI orchestration, real-time infrastructure for communication, and media handling for audio streaming. On Ikon, one person builds the entire thing as a single project.

**Interface and AI logic are in the same code.** You write the AI orchestration and the interface rendering in the same server code. When an AI analysis completes, the interface shows the result directly. An app can show streaming text, tool call progress, and intermediate results as they happen, without a round-trip through a separate layer.

## How the interface works

In Ikon, you write your interface in the same server-side code as the rest of your logic, so it can use your backend services, your data, and your AI calls directly. There's no separate frontend language or framework to learn.

Here is a complete chat interface. Everyone sees the same messages, and each user has their own input field:

```csharp
UI.Root(content: view =>
{
    // Shared state — all clients see the same messages
    foreach (var msg in _messages.Value)
    {
        view.Text(msg.Content);
    }

    // Per-client state — each client has their own input
    view.TextField(
        value: _inputText.Value,
        onValueChange: async val => { _inputText.Value = val; },
        onSubmit: async () =>
        {
            _messages.Value.Add(new Message(_inputText.Value));
            _inputText.Value = "";
        });
});
```

When one person types "hello", only their text field shows it. When they submit, everyone sees the new message. The framework handles the difference between shared and personal state, so the developer writes no code for it.

This is different from traditional server-side rendering, where the server generates a page once and sends it. Ikon's server keeps a live interface tree and tracks which parts of it depend on which values. When data changes, it re-renders only the affected parts and sends a compressed diff to every connected client.

The interface updates automatically whenever data changes, whether the change comes from a background task, a finished AI call, or another person's action. You don't write event handlers, synchronize client-side state, or reconcile optimistic updates.

## Three scopes of reactive state

Ikon's multiuser behavior comes from its reactive values. There are three kinds, each with a different scope:

### Shared across all clients

When any person adds a message, every connected person sees it immediately. The server re-renders the relevant portion of the interface for each client and sends the updates. Use this scope for shared application state, such as a collaborative document, a chat room, or a live scoreboard.

### Per-connection state

Each connection gets its own independent value. One person can type a search query without affecting anyone else's view. The sidebar can be open for one person and closed for another. Ikon keeps each connection's value separate automatically.

### Per-user across devices

If someone connects from both their laptop and phone, both connections share the same value. If they change the theme on one device, it changes on the other too. The value belongs to the user's identity, not to the connection.

## How it works

Ikon renders the interface once per connected client, each time with that client's scopes active. In each pass, shared values are the same for everyone, per-connection values differ for each connection, and per-user values are the same across one person's devices but differ between people.

So one interface description produces different results for different clients. This is how the chat example above shows each person their own text field and everyone the same messages, without an event bus, state synchronization code, or pub/sub.

## Bandwidth and performance

Server-driven interfaces raise fair questions about bandwidth and performance. Ikon keeps both in check in four ways:

**Differential updates**: The server keeps the previous interface tree for each client. After a value changes, it re-renders the affected subtree and computes a minimal diff. Only what changed is sent, not the entire tree.

**Automatic dependency tracking**: When the interface reads a value during rendering, the system records that dependency. When the value changes, only the parts of the interface that use it re-render.

**Compact binary format**: Interface diffs are serialized in a compact binary format and optionally compressed before transmission. A typical update is a few hundred bytes.

**Server-compiled styling**: Ikon compiles CSS on the server and sends only the styles the current interface uses. The client receives no unused styles and compiles nothing.

## Crosswind: styling and animation

Ikon styles interfaces with Crosswind, a utility-first styling system that is compatible with Tailwind's approach and adds styling utilities for declarative animation. They cover keyframe animations, per-letter and per-word text animations, staggered delays, easing functions, 3D transforms, and filter animations. You describe how something looks and how it moves in the same place, so you don't need separate CSS files or JavaScript animation libraries.

## Three scopes replace several state systems

Ikon has shared state for what everyone should see, per-connection state for each person's own interface controls, and per-user state that follows someone across devices. These three scopes replace the state management stores, real-time event handlers, session management, and cross-device sync that traditional stacks require. You declare the scope of each value, and Ikon keeps it in sync.
