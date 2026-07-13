<!-- mined-from: Architect -->
# Multi-Agent Parallel Discussion — Fan Out Per Persona

User submits one prompt. The app fires N independent `Emerge.Run<T>` calls — one per active agent persona — in parallel via `Task.WhenAll`, then drops every response into a shared message list with the agent's id and timestamp. A Summarize button does a follow-up Claude pass over the whole transcript and posts a special "summary" pseudo-message.

## When to use

Multi-disciplinary critique tools, debate apps, jury simulations, brainstorming where you want the user to hear several perspectives at once. Choose `WhenAll` over sequential when responses don't need to react to each other inside a single turn — each agent reads the running transcript on the next turn anyway.

## Snippet

```csharp
private async Task HandleSendMessage()
{
    if (string.IsNullOrWhiteSpace(_userMessage.Value)) return;

    var userMsg = _userMessage.Value;
    _userMessage.Value = "";

    _discussionMessages.Add(new DiscussionMessage(
        Guid.NewGuid().ToString(), "user", "User", userMsg, DateTime.UtcNow));

    _isThinking.Value = true;

    try
    {
        var landscapeContext = BuildLandscapeContext();
        var activeAgents = _activeAgentIds.Value
            .Select(id => AvailableAgents.FirstOrDefault(a => a.Id == id))
            .Where(a => a != null)
            .ToList();

        // Run all agents in parallel
        var tasks = activeAgents.Select(agent =>
            GetAgentResponse(agent!, userMsg, landscapeContext)).ToList();

        var responses = await Task.WhenAll(tasks);

        _discussionMessages.AddRange(
            Enumerable.Range(0, activeAgents.Count).Select(i =>
                new DiscussionMessage(
                    Guid.NewGuid().ToString(),
                    activeAgents[i]!.Id, activeAgents[i]!.Name,
                    responses[i], DateTime.UtcNow)));
    }
    finally
    {
        _isThinking.Value = false;
    }
}

private async Task<string> GetAgentResponse(Agent agent, string userMessage, string landscapeContext)
{
    var systemPrompt = $"""
        You are {agent.Name}, a {agent.Role}.
        Your expertise: {agent.Specialty}
        Your persona: {agent.Persona}
        {landscapeContext}
        Other specialists may have different perspectives — engage constructively.
        """;

    var result = await Emerge.Run<AgentResponse>(
        LLMModel.Claude45Sonnet,
        pass =>
        {
            pass.SystemPrompt = systemPrompt;
            pass.Command = $"User: {userMessage}\n\nProvide your expert perspective. Return JSON:\n{pass.JsonSchema}";
            pass.Temperature = 0.7;
            pass.MaxOutputTokens = 1000;
        });

    return result.Response;
}
```

## Notes

- Each agent receives the same `landscapeContext` block which includes the recent transcript and the names of the other agents present — that's how disagreement and reference between perspectives emerges across turns.
- A single `_isThinking` flag covers all parallel calls; show one spinner at the bottom of the chat, not one per agent (avoid jitter).
- If one agent's call fails, you currently lose the whole batch under `WhenAll`. For production, replace with `Task.WhenAll(tasks.Select(t => t.ContinueWith(...)))` or `Task.WhenEach` to render responses as they land.
- Summarize is a separate, single-call pass — different prompt, lower temperature, claims its own pseudo-agent id ("summary") so it renders with a distinct accent in the message list.

## See also

- `agent-roster-card-grid` — the picker that populates the active set
- `chatbot-streaming` — single-agent streaming alternative for one-on-one chats
