# The App That Grew a Second Face

*Published 2026-04-16*

A developer ships a quiz game. Six players join on their phones, a host runs the round from a laptop, and the AI writes the questions during the game. It all runs in a browser tab.

Then someone says, "Can we put this on the TV in the break room?"

The break room TV runs a Flutter app. The browser cannot help there. The developer looks at the codebase — 400 lines of C#, a handful of Tailwind styles, no frontend code worth mentioning — and wonders how much of it needs to be rewritten.

None of it. The quiz game gets a second frontend, in Flutter, without a single change to its server code.

## The server does not know which client renders the UI

Ikon Parallax apps describe their UI in C# on the server. A button is described by its label, its style and its onClick handler. The server does not know whether that button becomes a `<button>` in HTML or a `TextButton` in Flutter. It builds a tree of components, compares it with the last version, and sends the changes to every connected client.

```csharp
view.Button(
    ["px-4 py-2 bg-blue-500 rounded-lg text-white"],
    text: "Buzz In!",
    onClick: async () => _buzzedIn.Value = clientId);
```

A web client receives this and injects CSS. A Flutter client receives the same tree, but its style data is `EdgeInsets(left: 16, top: 8, right: 16, bottom: 8)` instead of `padding: 1rem`. The server resolves the same Tailwind classes into both formats and sends each client only the format it understands.

The developer does not have to handle any of this. They write `px-4`, and both platforms get the right padding.

## A folder and a pubspec

Adding Flutter to an existing app means creating a `frontend-flutter/` directory next to the existing `frontend-node/`. It contains a `pubspec.yaml` that depends on the Ikon SDK and a `main.dart` that connects to the server and mounts a single widget.

That widget, `IkonParallaxView`, subscribes to the server's UI stream, resolves styles against a built-in Tailwind color palette, and maps each component to its Flutter equivalent. A row becomes a `Row`, a column becomes a `Column`, and a scroll area becomes a `SingleChildScrollView`. Every component has a fixed Flutter counterpart, so the mapping needs no per-app code.

When the server changes a single text node, for example from "Score: 5" to "Score: 6", it sends only that change, not the whole tree. The Flutter client updates the existing node in place, so only one widget is rebuilt and the rest of the screen stays as it is.

## The TV, the phone, and the laptop

Back to the break room. The quiz game now has three kinds of client connected at the same time. The host's laptop runs the web frontend and shows the admin panel with question controls. The six phones also run the web frontend and show the players' buzzers. The TV runs the Flutter app and shows the scoreboard and current question in a layout designed for a big screen.

All of them are connected to the same server and the same session, and they share the same reactive state. When a player buzzes in, the TV updates instantly. When the host reveals the answer, every phone shows the result. The server handles every client the same way, whatever its platform. It sends the UI tree, and each client renders it.

The developer wrote one app and added a second frontend when the TV needed one.

## What stays on the server

Audio with echo cancellation, video calls, screen sharing, AI inference, database queries and authentication all run on the server. The Flutter client does as little as the browser client. It renders the UI the server sends and forwards the user's input to the server. Push-to-talk is a gesture detector that starts and stops a mic stream. A share button opens the platform's native share sheet. The server decides what to show, when to listen and what to say.

So the app's logic, AI calls and state are all in one place, on the server, and the client that draws the UI is the small part. Today the clients are a browser and a Flutter app. Later a client could be a game engine, a car dashboard or a teddy bear. The server works the same way for each one. It builds a UI tree and sends it over the network.

The quiz game started as a weekend project. It now runs from one codebase on two platforms, on six phones, a laptop and a TV in a break room, and none of it had to be rewritten.
