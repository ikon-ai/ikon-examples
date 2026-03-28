# The Claude Code Development Loop

Open two terminals. In the first, run `ikon app run`. In the second, start Claude Code. That is the entire development environment. No IDE required. No build step. No restart cycle. You describe what you want, the AI writes it, the running app hot-reloads, and you see the result immediately. Then you describe what to change, and the loop repeats.

This is not a hypothetical workflow. It is how several of the apps in this series were built -- and it changes the economics of what one person can build in an afternoon.

## Creating an app

Everything starts with one command:

```bash
cd platform-dotnet && CI=true ikon app new Ikon.App.MyProject
```

This creates a complete project: a C# application file, a frontend shell, configuration, and a solution file. The scaffolded app compiles and runs immediately -- a blank canvas with the full Ikon runtime available. Reactive state, server-driven UI, AI orchestration, audio, video, multiplayer -- all accessible from the first line of code you write.

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
public class MyProjectApp(IApp<SessionIdentity, ClientParameters> host)
{
    private UI UI { get; } = new(host, new Theme());

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

One file. One class. A heading on screen. Everything else is available when you need it.

## The two-terminal setup

Terminal one runs the app:

```bash
cd platform-dotnet/Ikon.App.MyProject && ikon app run
```

This starts the .NET server and the Vite frontend dev server together. The app is accessible at `http://localhost:5000`. Both processes watch for file changes.

Terminal two is Claude Code. You open it in the project directory and start describing what you want to build. Claude reads the app file, understands the Ikon APIs, and writes code directly into the source files. When it saves a change, the running server detects the modification and hot-reloads.

The key detail: all `Reactive<T>` state survives the reload. If you had a list of items on screen and Claude adds a search filter, the items are still there after the reload. The app does not reset to a blank state every time the code changes. This is what makes the loop feel continuous rather than start-stop.

## What hot reload preserves

Ikon has four tiers of reactive state, and all of them survive hot reload:

- `Reactive<T>` -- shared across all connected clients
- `ClientReactive<T>` -- scoped to a single browser session
- `UserReactive<T>` -- scoped to an authenticated user
- `PersistentReactive<T>` -- additionally saved to cloud storage

When the server reloads, it serializes all reactive state to a temporary JSON file, restarts the process, and restores the state before the app's `Main()` runs again. From the user's perspective, the page flickers briefly and comes back with the same data.

Non-reactive fields -- plain instance variables, background tasks, timers -- do not survive. This is by design. If you need state to persist across reloads, declare it as `Reactive<T>`. If you have background work, use `host.StoppingAsync` to clean up gracefully when a reload occurs.

## What the loop looks like in practice

Here is a realistic session. You start with the scaffolded app and build a tool that lets users paste a URL, scrapes the content, and generates a summary with key takeaways.

**You:** "Add a text field for a URL and a button that says Analyze. When clicked, show a loading state."

Claude reads the app file, adds a `Reactive<string>` for the URL, a `Reactive<bool>` for loading state, a `TextField`, and a `Button` with an `onClick` handler. The app hot-reloads. You see the text field and button appear in the browser.

**You:** "When the button is clicked, scrape the URL content and generate a summary with three key takeaways. Use Claude Sonnet."

Claude adds the scraping call and an AI generation call with structured output -- a C# record with `Summary` and `List<string> Takeaways` fields. It wires the result into new reactive state and adds UI to display it. The app reloads. You paste a URL, click Analyze, and watch the summary appear.

**You:** "The takeaways should be in cards with a subtle border. Add a copy button for the summary."

Claude modifies the UI declarations. Reload. The cards appear. The copy button works.

**You:** "Make it remember the last five URLs analyzed so you can go back to previous results."

Claude adds a `Reactive<List<AnalysisResult>>` for history, a sidebar showing previous URLs, and click handlers to restore previous results. Reload. The history appears, populated with the analysis you just ran -- because reactive state survived the reload.

Each iteration takes seconds. The feedback loop is: describe, write, reload, see. No compilation wait. No browser refresh. No lost state.

## Why this works better than a traditional setup

The traditional version of this workflow involves an IDE, a terminal for the dev server, a browser, and frequent context switches between them. The AI assistant generates code, you paste it into the right file, the dev server rebuilds, you switch to the browser, you check the result, you switch back to the chat, you describe the next change.

With Claude Code and `ikon app run`, the loop collapses. Claude writes directly to the files. The server reloads automatically. You stay in one place -- the terminal -- and check the browser when you want to see the result. The AI handles the editing. The platform handles the reloading. You handle the direction.

The other factor is that Ikon apps are server-driven. The UI is declared in the same C# file as the logic. There is no separate frontend to modify, no API endpoints to wire up, no client-side state to synchronize. When Claude changes the app file, it changes everything -- the interface, the logic, the AI orchestration, the data model. One file, one reload, one result.

## What Claude Code brings to the loop

Claude Code is not just a code generator pasting snippets. It reads the existing codebase, understands the current state of the app, and makes targeted modifications. When you say "add a dark mode toggle," it does not generate a new app from scratch. It reads the current theme, adds a `ClientReactive<bool>` for the preference (per-client, so each viewer gets their own setting), adds a toggle button, and modifies the theme instantiation to respect the preference.

It also reads the Ikon platform documentation when it needs to understand an API. How to call an AI model with structured output. How to add speech recognition. How to use the Emergence patterns for multi-agent orchestration. The documentation is available as context, so Claude can use APIs correctly on the first try rather than guessing and iterating.

When something goes wrong -- a compilation error, a runtime exception -- the error appears in the terminal where `ikon app run` is running. Claude can read the server logs, diagnose the issue, and fix it. The loop continues.

## From prototype to production

The app you build in this loop is not a throwaway prototype. It is the production app. The same code, the same reactive state model, the same AI orchestration runs when you deploy:

```bash
ikon app config    # connect to your cloud environment
ikon app deploy    # bundle and deploy
```

The deployed app runs as a persistent process on Ikon's infrastructure. It handles multiplayer automatically. It scales. The hot reload state mechanism is replaced by proper server lifecycle management, but the reactive state model is identical. What worked on localhost works in production.

There is no "now rewrite it properly" step. The code Claude wrote, the reactive state it declared, the AI calls it wired up -- that is the production code. The development loop and the deployment target are the same platform.

## The ceiling

This workflow is not limited to simple tools. The same two-terminal loop produced apps with:

- Multi-model AI orchestration using Emergence patterns -- draft, critique, verify loops running across different models
- Real-time audio with speech recognition and text-to-speech
- Multiplayer collaboration where every connected user sees live updates
- Background processing that continues when all browsers close
- Image and video generation pipelines
- Persistent state that survives not just hot reloads but full server restarts

Each of these capabilities is available as a single API call or a few lines of code. Claude knows the APIs. The hot reload preserves the state. The loop stays fast regardless of how complex the app becomes.

Two terminals. One for the runtime, one for the AI. Everything else follows.
