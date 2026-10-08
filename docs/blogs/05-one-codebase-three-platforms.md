# One Codebase, Three Platforms

*Published 2026-03-19*

Imagine a training simulation with a 3D environment in a game engine, an instructor dashboard in a browser, and sensor data streaming from a device on a kiosk. These are three platforms with three rendering technologies, and normally three teams would build and maintain a separate backend for each. On Ikon, all three connect to one application, share the same live state, and update at the same time when anything changes.

## What you can build

**Reach web, game engines, and hardware from one application.** You write the logic once, on the server. A browser client, a Unity game, and a native device all connect to the same running application and see the same state, so there is no second copy of the logic to keep in sync.

**Companion apps and cross-device experiences with no extra backend.** To add a web dashboard to your game or a mobile companion to your desktop tool, connect another client. Both clients see the same shared state, updated in real time.

**AI features in games without shipping API keys in game builds.** The game client sends player context to the server, which runs all AI logic and returns responses. Credentials and models stay on the server and are never bundled into a build that ships to end users.

**One deployment updates every client instantly.** To fix a bug, add a feature or swap an AI model, you deploy the server once, and every connected client on every platform gets the change immediately. Because the logic is on the server, these changes don't wait for app store review, and the Unity build can't fall two versions behind the web client.

**AI on native and embedded devices.** AI-powered decision support works on constrained hardware such as kiosks, medical devices and industrial controllers, because the AI runs on the server. The client only needs to implement the protocol.

## The usual tradeoff (and how Ikon avoids it)

Building a multiplatform application usually means accepting limits or duplicating work. If you write it in JavaScript, you accept limitations on desktop and native. If you write it natively for each platform, you maintain three codebases. If you use a cross-platform framework, you have to work around it wherever its abstraction doesn't match the platform.

Ikon avoids this choice. The application logic runs on the server, and the client only renders what the server sends. The server communicates through one binary protocol called Teleport, so any client that implements the protocol can connect, whatever platform it runs on.

## The protocol is the API

In a traditional architecture, the API contract is defined by HTTP endpoints, request schemas, and response formats. Every client platform needs to implement the same HTTP calls, handle the same error codes, parse the same JSON responses.

In Ikon, the contract is the Teleport protocol, a compact binary message format with typed opcodes for UI, audio, video, and events. A client has to connect, authenticate, and send and receive messages. The protocol defines everything else.

The server does not need to know what kind of client is connected. A browser, a Unity game, and an embedded device all authenticate the same way, receive the same messages, and display the same UI. Your application logic never branches by platform.

## Three native SDKs, one protocol

Ikon has native SDKs for three platforms:

**TypeScript -- for web and Node.js.** Runs in browsers and server-side JavaScript. Handles transport negotiation, audio processing, and two-way communication between client and server. Browser-based Ikon applications use this SDK.

**C# -- for .NET and Unity.** Targets both .NET applications and the Unity game engine. Supports automatic reconnection, built-in audio handling, and event-driven lifecycle management. Because it targets Unity, you can use the SDK directly in a game project for AI-powered NPCs, procedural content, dialogue systems, or companion apps alongside the game.

**C++ -- for native and embedded.** A small library with no external dependencies, so it can be built into game engines, embedded systems, desktop applications and industrial hardware. Any device that can run C++ can connect to an Ikon server.

All three SDKs do more than receive UI updates. The server can call functions registered on the client, for example to access a camera, query a game's inventory or read sensor data. Audio streams in both directions for voice applications, and any platform can stream video to the server for AI analysis.

## What it looks like in practice

**AI-powered game features.** A Unity game connects to an Ikon server running AI logic. The server handles NPC dialogue, procedural quest generation, or adaptive difficulty. The game sends player context, and the server returns responses and UI overlays. The game team works on the game and the AI team works on the AI, both against the same server.

**Hardware integration.** A native application running on a kiosk or specialized device connects to an Ikon server for AI-powered decision support. The device handles its hardware, and the server runs the AI.

**Cross-platform collaboration.** A desktop application and a browser client connect to the same application instance. They see each other's activity and share state in real time. Nobody built a collaboration layer for this, because state is shared by default.

**Companion applications.** A web dashboard and a mobile app connect to the same running Ikon server. Both see the same live AI analysis, the same monitoring data, the same collaborative document. Building the second client only means connecting it through one of the SDKs.

## Where the application runs

The application is one codebase on the server, clients use one of three SDKs, and the Teleport protocol connects them. The same application serves web users, game players and hardware devices, without a separate backend, API or deployment pipeline for each. You don't build cross-platform support as a separate feature. You get it because the application runs on the server.
