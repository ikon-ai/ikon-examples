<!-- This file is automatically updated by ikon tool commands. User edits are preserved only below the ikon-user-content-below marker. -->

# Ikon AI App Development Guidelines

**Before writing code, read the relevant guide files under `docs/guides/`. Each guide contains current API signatures generated from source code. The examples in guides are illustrative — always verify method signatures against the embedded API references.**

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

## Guides

**Read the relevant guide before writing code. Each guide contains current API signatures embedded from source.**

| Guide | Read when working with |
|-------|----------------------|
| [App Structure](docs/guides/app-structure.md) | app file structure, session identity, client parameters, partial class, global usings, lifecycle, host services, navigation, background work, client functions, messages |
| [Reactive State](docs/guides/reactive-state.md) | reactive, client reactive, user reactive, persistent reactive, reactive scope, value mutation |
| [UI Components](docs/guides/ui-components.md) | layout, overlays, inputs, display, navigation, drag-and-drop, text, button, dialog, tabs, accordion, scroll area, toast, popover |
| [Styling & Motion](docs/guides/styling-and-motion.md) | crosswind, tailwind, theme constants, style arrays, motion, animation |
| [AI & Emergence](docs/guides/ai-and-emergence.md) | emergence, emerge run, best of, map reduce, task graph, tree search, tools, image generation, speech, TTS, STT, classifier, embedding, web search, LLM |
| [Audio & Video](docs/guides/audio-video.md) | audio, video, capture, speech, stream |
| [Asset System](docs/guides/asset-system.md) | asset, cloud file, local file, cloud json, storage, metadata, URI, optimistic concurrency |
| [Endpoints & Databases](docs/guides/endpoints-databases.md) | endpoints, HTTP, HTTPS, WebSocket, databases, PostgreSQL |
| [Function Registry](docs/guides/function-registry.md) | function registry, registration, attribute, visibility, LLM tools, callable functions |
| [Logging](docs/guides/logging.md) | log, logging, debug, warning, error, diagnostics |
| [Pipelines](docs/guides/pipelines.md) | pipeline, background processing, transform, processor, scheduled, cron |
| [Frontend Fundamentals](docs/guides/frontend-fundamentals.md) | frontend, SDK, auth, connection, i18n, styling, query params |
| [Custom UI Components](docs/guides/frontend-custom-components.md) | custom UI component, module, resolver, React |
| [Config & CLI](docs/guides/config-and-cli.md) | ikon-config.toml, CLI commands, build, deploy, run, auth, activation, targets |


## Full API References

Guides embed filtered API snippets. For complete API surfaces, see:

- [Ikon.AI Public API](docs/Ikon.AI/public-api.md)
- [Ikon.AI.Emergence Public API](docs/Ikon.AI.Emergence/public-api.md)
- [Ikon.App Public API](docs/Ikon.App/public-api.md)
- [Ikon.Common Public API](docs/Ikon.Common/public-api.md)
- [Ikon.Common.Core Public API](docs/Ikon.Common.Core/public-api.md)
- [Ikon.Crosswind Public API](docs/Ikon.Crosswind/public-api.md)
- [Ikon.Parallax Public API](docs/Ikon.Parallax/public-api.md)
- [Ikon.Pipeline Public API](docs/Ikon.Pipeline/public-api.md)
- [Ikon.Pipelines.Public Public API](docs/Ikon.Pipelines.Public/public-api.md)
- [Ikon.Resonance Public API](docs/Ikon.Resonance/public-api.md)
- [Ikon.Resonance.Core Public API](docs/Ikon.Resonance.Core/public-api.md)
- [Ikon.Sdk Public API](docs/Ikon.Sdk/public-api.md)
- [Ikon.Teleport Public API](docs/Ikon.Teleport/public-api.md)


<!-- ikon-user-content-below -->
