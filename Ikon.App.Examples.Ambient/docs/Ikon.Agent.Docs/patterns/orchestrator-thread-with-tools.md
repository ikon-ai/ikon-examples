<!-- mined-from: Ikon.App.Lattice -->
# Orchestrator + Thread With Tools — Per-App Agent With Domain Tools

Construct an `Orchestrator`, register one `Persona` (system prompt + reasoning) whose `Skills` carry the app's domain tools — `Tool.Of` over plain C# methods returning strings — and fetch the app's single conversation thread with `CreateThreadAsync`. User messages go through `thread.PostAsync` + `ReactivateIfIdleAsync` + `DriveAsync`; the UI binds directly to the thread's reactives (`Messages`, `ToolCallTimeline`, `Status`) — no event pump needed.

## When to use

When the app has a clear conversational role with 4–10 deterministic tools (search, lookup, mutate-this-view) — and you want the LLM to choose them on its own. This is the standard shape for "intelligent assistant for a domain database" apps.

## Snippet

```csharp
private const string PersonaName = "oiva";

private Orchestrator _mind = null!;
private AgentThread _thread = null!;

private async Task InitializeMindAsync()
{
    _mind = new Orchestrator();
    _mind.AddPersona(new Persona(
        Name: PersonaName,
        SystemPrompt: "You are Oiva — a building maintenance assistant. "
            + "Use the tools to ground every claim. Reply briefly.",
        Skills: [new BuildingSkill(this)],
        Reasoning: new Reasoning(Capability.Standard, ModelFamily.Claude, Temperature: 0.4, MaxOutputTokens: 8000)));
    await _mind.ResumeAsync();

    _thread = await _mind.CreateThreadAsync(
        PersonaName,
        new Content.Text("Help the user inspect and maintain their buildings."),
        appName: "Oiva",
        planName: "session");
}

public sealed class BuildingSkill(OivaApp app) : Skill
{
    public override string Name => "buildings";

    public override string Instructions =>
        "Search first, then answer from tool results only — never invent building data.";

    public override IEnumerable<Tool> Tools() =>
    [
        Tool.Of<string, string>("search_building",
            "Search buildings by name/address/decade. Returns matches.",
            query => Task.FromResult(app.SearchBuildings(query))),

        Tool.Of<string, string>("select_building",
            "Switch the UI to the given building id.",
            id => Task.FromResult(app.SelectBuilding(id))),
    ];
}

private async Task PostUserMessageAsync(string text)
{
    await _thread.PostAsync(new Message(Author.User, [new Content.Text(text)]));
    await _thread.ReactivateIfIdleAsync();
    await _thread.DriveAsync(DriveMode.UntilQuiescent);
}

private void RenderChat(UIView view)
{
    var turns = _thread.Messages.Value
        .Where(m => m.Author == Author.User || m.Author == Author.Agent(PersonaName))
        .ToList();

    foreach (var (message, index) in turns.Select((m, i) => (m, i)))
    {
        var isUser = message.Author == Author.User;
        view.Box(["rounded-xl p-3", isUser ? "bg-foreground/10 self-end" : "bg-card self-start"],
            key: index.ToString(),
            content: v => v.Text(["whitespace-pre-wrap text-sm"], message.GetText()));
    }

    foreach (var call in _thread.ToolCallTimeline.Value.Where(c => c.ResultText is null))
    {
        view.Text([Text.Caption], $"-> {call.ToolName}…");
    }

    if (_thread.Status.Value == ThreadStatus.Active)
    {
        view.Spinner();
    }
}
```

## Notes

- One `Persona` per app role. Construct the `Orchestrator` once in `Main`, `AddPersona`, then `ResumeAsync` before creating threads. Multiple personas on one orchestrator is for multi-role orchestration, not the common case.
- `CreateThreadAsync(personaName, seedTask, appName:, planName:)` is get-or-create by name: after `ResumeAsync` the same call returns the SAME persisted thread, and `seedTask` is posted only when the plan is first created — never onto an existing history. The default `Orchestrator()` uses in-memory storage; pass an `IStorage` backend for history that survives restarts.
- Tools are plain C# delegates via `Tool.Of<T1, TResult>` — the parameter schema is inferred from the signature, and a record parameter type gives the LLM named fields. Tools return strings (often pre-formatted multi-line text) — the LLM is happy to re-summarize them. Use `Tool.OfContext<…>` when the body needs the live `ToolContext` (write artifacts, read remaining budget).
- No event pump: `Messages`, `ToolCallTimeline`, `Status`, and `Activity` are `Reactive<T>` — render code just reads `.Value` and re-renders on change. Reach for `thread.Events` only for token-level streaming (`ThreadEvent.TextDelta`).
- `ReactivateIfIdleAsync` replaces any hand-rolled status-transition table: it re-engages a thread paused in Idle/WaitingForInput/Done/Failed and no-ops (returns false) when already Active. `DriveAsync(DriveMode.UntilQuiescent)` then runs passes until the thread settles back to Idle; concurrent drives coalesce, so calling it from every send-button press is safe.
- A tool that mutates UI (`select_building` flips a `Reactive`) is fine and is the cleanest way to let the agent drive navigation.
- `Author` is a struct with equality by (Kind, Name): compare against `Author.User` / `Author.Agent(name)`, or filter with `message.Author.Kind == AuthorKind.Agent`. `message.GetText()` joins the text parts of a multimodal message.
- `ToolCallTimeline` entries carry `ArgsJson`, `ResultText` (null while pending), `IsError`, and `PrecedingAgentMessages` — enough to slot completed tool-call rows inline in front of the agent message they produced.

## See also

- `chat-with-tool-calls`
- `chatbot-streaming`
