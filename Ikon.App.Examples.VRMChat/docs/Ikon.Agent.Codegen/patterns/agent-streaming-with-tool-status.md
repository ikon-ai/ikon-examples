<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Agent Streaming With Tool Status — One Loop, Three Reactives

Drive a chat agent through `Emerge.Run<T>` and demultiplex the event stream into three reactives: `_streamingText` (live token-by-token reply), `_statusText` (humanised "Searching cars..." pill while tools run), `_messages` (committed history). The user never sees a frozen UI between tool call and reply.

## When to use

Any LLM chat where the agent uses tools mid-response and you want the user to feel forward motion: typing, then a tool pill, then more typing, then the final message. Replaces a single "thinking..." spinner.

## Snippet

```csharp
await foreach (var ev in Emerge.Run<AgentResponse>(LLMModel.Claude45Sonnet, _conversationContext, pass =>
{
    pass.SystemPrompt = systemPrompt;
    pass.Command = conversationHistory;
    pass.Temperature = 0.7;
    pass.MaxOutputTokens = 1024;
    pass.MaxIterations = 10;

    pass.AddTool("search_inventory", "Semantic search ...",
        async (string? query, int? maxPrice, int? minYear) =>
            await _apiClient.SearchInventoryAsync(showroom, query, maxPrice, minYear));

    pass.AddTool("get_vehicle_images", "Image URLs by id.",
        async (string vehicleId) => await _apiClient.GetVehicleImagesAsync(showroom, vehicleId));
}))
{
    switch (ev)
    {
        case ModelText<AgentResponse> t:
            _streamingText.Value += t.Text;
            break;

        case ToolCallPlanned<AgentResponse> call:
            _statusText.Value = call.Call.Function.Name switch
            {
                "search_inventory"   => "Searching cars...",
                "get_vehicle_images" => "Loading images...",
                _                    => "Working..."
            };
            break;

        case Completed<AgentResponse> done:
            _conversationContext = done.Context;
            var final = done.Result?.Message ?? _streamingText.Value ?? "Sorry, try again?";
            _messages.Value = [.._messages.Value, new ChatMessage("assistant", final, DateTime.UtcNow)];
            _streamingText.Value = "";
            _statusText.Value = "";
            _ = ExtractProfileAsync();
            break;
    }
}
```

## Notes

- The streaming text is rendered as a transient assistant bubble (separate from `_messages`); on `Completed` it gets committed and `_streamingText` is cleared.
- Map tool function names to user-facing verbs in a `switch` expression — never expose raw function names.
- Persist `done.Context` back into `_conversationContext` so the next turn continues.
- Always replace the list (`= [..old, new]`), never `.Add` — `Reactive<List<>>` only diff-broadcasts on reference change.

## See also

- `streaming-agent-status`
- `chat-with-tool-calls`
- `parallel-extract-and-reply`
