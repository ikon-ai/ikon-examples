# The Claude Code Development Loop

*Published 2026-03-28*

Open two terminals. In the first, run `ikon run`. In the second, start Claude Code. That is the whole development environment, and you do not need an IDE, a build step or restarts. You describe what you want, the AI writes it, the running app hot-reloads, and you see the result right away. Then you describe the next change and repeat.

Several of the apps in this series were built this way. The workflow lets one person build much more in an afternoon.

## Creating an app

Everything starts with one command:

```bash
cd platform-dotnet && CI=true ikon new Ikon.App.MyProject
```

This creates a complete project: a C# application file, a frontend shell, configuration, and a solution file. The new app compiles and runs right away. It shows an almost empty page, and the full Ikon runtime is available to it. You can use reactive state, server-driven UI, AI orchestration, audio, video and multiplayer from the first line of code you write.

The result is a directory structure like this:

```
Ikon.App.MyProject/
├── app/Ikon.App.MyProject/
│   ├── MyProjectApp.cs          # Your app -- this is where you work
│   ├── GlobalUsings.cs
│   └── IkonTheme.cs
├── frontend-node/
│   └── src/main.cs
├── ikon-config.toml
└── Ikon.App.MyProject.slnx
```

The scaffolded app is minimal:

```csharp
return await App.Run(args);

[App]
public class MyProjectApp(IApp<SessionIdentity, ClientParameters> app)
{
    private UI UI { get; } = new(app, new Theme());

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column([Container.Xl2, "py-8 px-4"], content: view =>
            {
                view.Text([Text.H2], nameof(MyProjectApp));
            });
        });
    }
}
```

It is one file with one class, and it shows a heading on screen. You add the rest of the platform's features when you need them.

## The two-terminal setup

Terminal one runs the app:

```bash
cd platform-dotnet/Ikon.App.MyProject && ikon run
```

This starts the .NET server and the Vite frontend dev server together. The app is accessible at `http://localhost:5000`. Both processes watch for file changes.

Terminal two is Claude Code. You open it in the project directory and start describing what you want to build. Claude reads the app file, understands the Ikon APIs, and writes code directly into the source files. When it saves a change, the running server detects the modification and hot-reloads.

All `Reactive<T>` state survives the reload. If you had a list of items on screen and Claude adds a search filter, the items are still there after the reload. The app does not reset to a blank state every time the code changes. So you keep working on the same running app instead of starting over after each change.

## What hot reload preserves

Ikon has four kinds of reactive state, and all of them survive hot reload:

- `Reactive<T>` -- shared across all connected clients
- `ClientReactive<T>` -- scoped to a single browser session
- `UserReactive<T>` -- scoped to an authenticated user
- `PersistentReactive<T>` -- additionally saved to cloud storage

When the server reloads, it serializes all reactive state to a temporary JSON file, restarts the process, and restores the state before the app's `Main()` runs again. From the user's perspective, the page flickers briefly and comes back with the same data.

Non-reactive fields, such as plain instance variables, background tasks and timers, do not survive. If you need state to persist across reloads, declare it as `Reactive<T>`. If you have background work, use `app.StoppingAsync` to clean up gracefully when a reload occurs.

## What the loop looks like in practice

Here is an example session. You start with the scaffolded app and build a tool that lets users paste a URL, scrapes the content, and generates a summary with key takeaways.

**You:** "Add a text field for a URL and a button that says Analyze. When clicked, show a loading state."

Claude reads the app file, adds a `Reactive<string>` for the URL, a `Reactive<bool>` for loading state, a `TextField`, and a `Button` with an `onClick` handler. The app hot-reloads. You see the text field and button appear in the browser.

**You:** "When the button is clicked, scrape the URL content and generate a summary with three key takeaways. Use Claude Sonnet."

Claude adds the scraping call and an AI generation call with structured output, which is a C# record with `Summary` and `List<string> Takeaways` fields. It stores the result in new reactive state and adds UI to display it. The app reloads. You paste a URL, click Analyze, and watch the summary appear.

**You:** "The takeaways should be in cards with a subtle border. Add a copy button for the summary."

Claude changes the UI declarations and the app reloads. The cards appear, and the copy button works.

**You:** "Make it remember the last five URLs analyzed so you can go back to previous results."

Claude adds a `Reactive<List<AnalysisResult>>` for history, a sidebar showing previous URLs, and click handlers to restore previous results. The app reloads. The history already contains the analysis you just ran, because reactive state survived the reload.

Each round takes seconds: you describe a change, Claude writes it, the app reloads and you see it. You do not wait for compilation, refresh the browser or lose state.

## Why this works better than a traditional setup

The traditional version of this workflow involves an IDE, a terminal for the dev server, a browser, and frequent context switches between them. The AI assistant generates code, you paste it into the right file, the dev server rebuilds, you switch to the browser, you check the result, you switch back to the chat, you describe the next change.

With Claude Code and `ikon run`, most of those steps go away. Claude writes directly to the files and the server reloads automatically. You stay in the terminal and look at the browser when you want to see the result. Claude edits the code, the platform reloads the app, and you decide what to build next.

Ikon apps are also server-driven, and the UI is declared in the same C# file as the logic. You have no separate frontend to change, no API endpoints to connect and no client-side state to keep in sync. One change to the app file can change the interface, the logic, the AI orchestration and the data model, and one reload shows all of it.

## What Claude Code brings to the loop

Claude Code reads the existing code, works out the current state of the app and changes only what the request needs. When you say "add a dark mode toggle," it does not generate a new app. It reads the current theme, adds a `ClientReactive<bool>` for the preference (per-client, so each viewer gets their own setting), adds a toggle button, and modifies the theme instantiation to respect the preference.

It also reads the Ikon platform documentation when it needs to understand an API, for example how to call an AI model with structured output, how to add speech recognition or how to use the Emergence patterns for multi-agent orchestration. Because the documentation is available as context, Claude can use APIs correctly on the first try instead of guessing and retrying.

When something goes wrong, such as a compilation error or a runtime exception, the error appears in the terminal where `ikon run` is running. Claude can read the server logs, find the cause and fix it.

## From prototype to production

The app you build this way is the production app, not a prototype you throw away. When you deploy, the same code, reactive state model and AI orchestration run:

```bash
ikon link    # connect to your cloud environment
ikon deploy    # bundle and deploy
```

The deployed app runs as a persistent process on Ikon's infrastructure. It handles multiplayer automatically. It scales. In production the normal server lifecycle replaces the state handling that hot reload uses, but the reactive state model is identical. What worked on localhost works in production.

You do not rewrite the app properly afterwards. The code Claude wrote, including the reactive state and the AI calls, is the production code, because you develop and deploy on the same platform.

## What you can build this way

This workflow is not only for simple tools. The same two-terminal setup produced apps with:

- Multi-model AI orchestration using Emergence patterns, such as draft, critique and verify loops running across different models
- Real-time audio with speech recognition and text-to-speech
- Multiplayer collaboration where every connected user sees live updates
- Background processing that continues when all browsers close
- Image and video generation pipelines
- Persistent state that survives not just hot reloads but full server restarts

Each of these features is a single API call or a few lines of code. Claude knows the APIs and hot reload keeps the state, so each round stays fast however complex the app becomes.
