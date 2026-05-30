<!-- mined-from: Oiva.Agent -->
# Orchestrator + Thread With Tools — Per-App Agent With Domain Tools

Wire `Orchestrator.Create` with one `Agent` (system prompt + brain) and a set of `b.AddToolAsync` registrations that are just C# methods returning strings. Create a single thread on app start, then subscribe to `_mind.Events()` to fan `MessageAppended` and `ToolCallPlanned` events into reactive UI state. User messages go through `Threads.AppendMessageAsync` followed by a `TransitionAsync` whose enum varies by current `ThreadStatus`.

## When to use

When the app has a clear conversational role with 4–10 deterministic tools (search, lookup, mutate-this-view) — and you want the LLM to choose them on its own. This is the standard shape for "intelligent assistant for a domain database" apps.

## Snippet

```csharp
private async Task InitializeMindAsync()
{
    var agent = new Agent
    {
        Name = "oiva",
        SystemPrompt = "You are Oiva — a building maintenance assistant. " +
                       "Use the tools to ground every claim. Reply briefly.",
        Reasoning = Reasoning.Standard,
    };

    _mind = await Orchestrator.Create(b =>
    {
        b.WithMemoryStorage().AddAgent(agent);
        b.AddToolAsync<string, string>("search_building",
            "Search buildings by name/address/decade. Returns matches.",
            async query => await Task.FromResult(SearchBuildings(query)));
        b.AddToolAsync<string, string>("select_building",
            "Switch the UI to the given building id.",
            async id => await Task.FromResult(SelectBuildingTool(id)));
        b.AddToolAsync<string>("building_summary",
            "Return a summary of the selected building.",
            async () => await Task.FromResult(BuildingSummary()));
    });

    var thread = await _mind.CreateThreadAsync("Conversation", agentDef: "oiva");
    _threadId = thread.Id;

    _ = Task.Run(async () =>
    {
        await foreach (var evt in _mind.Events())
        {
            if (evt.ThreadId != _threadId) continue;
            if (evt is MessageAppended ma && ma.Message.Author is not MessageAuthor.SystemAuthor)
            {
                _chatTurns.Value = [.. _chatTurns.Value, new ChatTurn
                {
                    Author = ma.Message.Author == MessageAuthor.User ? "user" : "agent",
                    Content = ma.Message.Content,
                }];
            }
            else if (evt is ToolCallPlanned tcp)
            {
                _chatTurns.Value = [.. _chatTurns.Value, new ChatTurn
                    { Author = "tool", Content = $"-> {tcp.ToolName}" }];
            }
            else if (evt is AgentStarted) _isChatBusy.Value = true;
            else if (evt is AgentCompleted) _isChatBusy.Value = false;
        }
    });
}

private async Task PostUserMessageAsync(string text)
{
    _isChatBusy.Value = true;
    await _mind.Threads.AppendMessageAsync(_threadId, MessageAuthor.User, text);
    var info = _mind.Threads.GetThread(_threadId);
    var transition = info?.Status switch
    {
        ThreadStatus.WaitingForInput => ThreadTransition.ReactivateFromAsk,
        ThreadStatus.Idle            => ThreadTransition.ReactivateFromIdle,
        ThreadStatus.Waiting         => ThreadTransition.ReactivateFromWait,
        _                            => ThreadTransition.Reactivate,
    };
    await _mind.Threads.TransitionAsync(_threadId, transition);
}
```

## Notes

- One `Agent` per app role. Multiple agents in one Orchestrator is for multi-role orchestration, not the common case.
- Tools return strings (often pre-formatted multi-line text) — the LLM is happy to re-summarize them.
- Subscribe to `_mind.Events()` on app start, not per-render — render only reads `_chatTurns.Value`.
- The transition table on user-message-post is needed because the thread can be in any status; using `Reactivate` blindly will fail for `WaitingForInput`.
- A tool that mutates UI (`select_building` flips a `Reactive`) is fine and is the cleanest way to let the agent drive navigation.
- `MessageAuthor` is a discriminated record. The static instances are `MessageAuthor.User` and `MessageAuthor.System` — that's what `==` compares against. The nested types (`MessageAuthor.UserAuthor`, `MessageAuthor.SystemAuthor`, `MessageAuthor.AgentAuthor`, `MessageAuthor.ThreadAuthor`) are for `is`-pattern matching only; `Author == MessageAuthor.UserAuthor` doesn't compile because `UserAuthor` is a type, not a value.

## See also

- `chat-with-tool-calls`
- `chatbot-streaming`
