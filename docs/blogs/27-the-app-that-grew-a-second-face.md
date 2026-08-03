# The App That Grew a Second Face

A developer ships a quiz game. Six players join on their phones, a host controls the round from a laptop, the AI invents questions on the fly. It runs beautifully in a browser tab.

Then someone says, "Can we put this on the TV in the break room?"

The break room TV runs a Flutter app. The browser cannot help there. The developer looks at the codebase — 400 lines of C#, a handful of Tailwind styles, no frontend code worth mentioning — and wonders how much of it needs to be rewritten.

None of it. The quiz game grows a Flutter face without changing a single line of server code.

## The server does not know what rendered you

Ikon Parallax apps describe their UI in C# on the server. A button is a button — it has a label, a style, and an onClick. The server does not know whether that button becomes a `<button>` in HTML or a `TextButton` in Flutter. It builds a tree of components, diffs it against the last version, and sends the changes to whoever is connected.

```csharp
view.Button(
    ["px-4 py-2 bg-blue-500 rounded-lg text-white"],
    text: "Buzz In!",
    onClick: async () => _buzzedIn.Value = clientId);
```

A web client receives this and injects CSS. A Flutter client receives the same tree but gets different style data — not `padding: 1rem` but `EdgeInsets(left: 16, top: 8, right: 16, bottom: 8)`. The server resolves the same Tailwind classes into both formats and sends each client only the one it understands.

The developer never thinks about this. They write `px-4` and both platforms get the right padding.

## A folder and a pubspec

Adding Flutter to an existing app means creating a `frontend-flutter/` directory next to the existing `frontend-node/`. Inside: a `pubspec.yaml` that depends on the Ikon SDK, and a `main.dart` that connects to the server and mounts a single widget.

That widget — `IkonParallaxView` — subscribes to the server's UI stream, resolves styles against a built-in Tailwind color palette, and maps each component to its Flutter equivalent. Rows become `Row`s. Columns become `Column`s. Scroll areas become `SingleChildScrollView`s. The mapping is mechanical and complete.

When the server updates a single text node — "Score: 5" becomes "Score: 6" — it sends a diff, not the whole tree. The Flutter client patches the existing node in place. One property changes, one widget rebuilds. The rest of the screen is untouched.

## The TV, the phone, and the laptop

Back to the break room. The quiz game now has three clients connected simultaneously. The laptop runs the web frontend — the host sees the admin panel with question controls. Six phones run the web frontend — players see their buzzers. The TV runs the Flutter app — it shows the scoreboard and current question in a layout designed for a big screen.

All of them are connected to the same server, the same session, the same reactive state. When a player buzzes in, the TV updates instantly. When the host reveals the answer, every phone shows the result. The server does not care which client is which platform. It sends the tree, the clients render it.

The developer wrote one app. It grew faces on demand.

## What stays on the server

Audio with echo cancellation, video calls, screen sharing, AI inference, database queries, authentication — all of it runs on the server. The Flutter client is as thin as the browser client: it renders what it is told and forwards what the user does. Push-to-talk is a gesture detector that starts and stops a mic stream. A share button invokes the platform's native share sheet. The server decides what to show, when to listen, what to say.

This is the point. The interesting part of an app — the logic, the AI, the state — lives in one place. The rendering surface is a detail. Today it is a browser and a Flutter app. Tomorrow it could be a game engine, a car dashboard, or a teddy bear. The server does not care. It builds a tree and sends it down the wire.

A quiz game that started as a weekend project. One codebase, two platforms, six phones, a laptop, and a TV in a break room. No rewrite required.
