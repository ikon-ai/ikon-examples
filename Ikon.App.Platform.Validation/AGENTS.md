<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# Ikon AI App Development Guidelines

**Detailed API references are in docs/guides/. See the guide index at the bottom of this document.**

## Architecture

Ikon AI App is the complete application containing compiled C# app and compiled JS frontend, uploaded as a single bundle.

- C# app runs in cloud, persists even when all clients disconnect
- External AI service calls happen in C# app, not from client
- Frontend is minimal thin client connecting to C# app in cloud
- C# app streams UI, styles, audio, video to clients
- Client sends user input (clicks, audio, video) to C# app
- Multiple clients can connect to same app instance for collaboration
- Architecture supports sending different UI to each client via reactive scopes

## Development Focus

- Most development happens in the C# app, including styling
- Crosswind styling: Tailwind-like syntax but extended, used directly in C# code
- UI declared reactively; changes to `Reactive<T>` values trigger updates automatically
- UI changes sent to clients as minimal diffs (server-side diffing)
- Only modify frontend-node when integrating custom UI components
- Developers can freely add any NuGet packages to C# app or npm packages to frontend

## Minimal App Template

```csharp
return await App.Run(args);

public record SessionIdentity(string UserId);
public record ClientParameters(string Name = "Ikon");

[App]
public class MyApp(IApp<SessionIdentity, ClientParameters> host)
{
    private UI UI { get; } = new(host, new Theme());
    private Audio Audio { get; } = new(host);

    private readonly Reactive<string> _name = new("");

    public async Task Main()
    {
        UI.Root([Page.Default], content: view =>
        {
            view.Column([Container.Xl2, "py-8 px-4"], content: view =>
            {
                view.Text([Text.H2], string.IsNullOrWhiteSpace(_name.Value) ? "Hello!" : $"Hello, {_name.Value}!");
                view.TextField([Input.Default], placeholder: "Enter your name", value: _name.Value,
                    onValueChange: async value => { _name.Value = value; });
            });
        });
    }
}
```

## API Reference Guides

Detailed API docs are available in `docs/guides/`. Each guide covers a specific topic:

- **app-structure** (`docs/guides/app-structure.md`): app file structure, session identity, client parameters, partial class, global usings, lifecycle, host services, navigation, background work, client functions, messages
- **reactive-state** (`docs/guides/reactive-state.md`): reactive, client reactive, user reactive, persistent reactive, reactive scope, value mutation
- **ui-components** (`docs/guides/ui-components.md`): layout, overlays, inputs, display, navigation, drag-and-drop, text, button, dialog, tabs, accordion, scroll area, toast, popover
- **styling-and-motion** (`docs/guides/styling-and-motion.md`): crosswind, tailwind, theme constants, style arrays, motion, animation, UI guidelines, theme customization
- **ai-and-emergence** (`docs/guides/ai-and-emergence.md`): emergence, emerge run, best of, map reduce, task graph, tree search, tools, image generation, speech, TTS, STT, classifier, embedding, web search, LLM
- **audio-video** (`docs/guides/audio-video.md`): audio, video, capture, speech, stream
- **asset-system** (`docs/guides/asset-system.md`): asset, cloud file, local file, cloud json, storage, metadata, URI, optimistic concurrency
- **endpoints-databases** (`docs/guides/endpoints-databases.md`): endpoints, HTTP, HTTPS, WebSocket, databases, PostgreSQL
- **function-registry** (`docs/guides/function-registry.md`): function registry, registration, attribute, visibility, LLM tools, callable functions
- **logging** (`docs/guides/logging.md`): log, logging, debug, warning, error, diagnostics
- **pipelines** (`docs/guides/pipelines.md`): pipeline, background processing, transform, processor, scheduled, cron
- **frontend-fundamentals** (`docs/guides/frontend-fundamentals.md`): frontend, SDK, auth, connection, i18n, styling, query params
- **frontend-custom-components** (`docs/guides/frontend-custom-components.md`): custom UI component, module, resolver, React
- **config-and-cli** (`docs/guides/config-and-cli.md`): ikon-config.toml, CLI commands, build, deploy, run, auth, activation, targets


<!-- ikon-user-content-below -->
