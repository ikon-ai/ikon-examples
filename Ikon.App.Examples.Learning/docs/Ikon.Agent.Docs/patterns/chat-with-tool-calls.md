<!-- mined-from: Sentrix -->
# Chat With Tool Calls — Streaming Reply That Loops Through Tools

A chat where the LLM can call read/write tools mid-reply. Each `Emerge.Run` event is dispatched: `ModelText` appends to the visible bubble, `ToolCallPlanned` flips the spinner on, `ToolCallResult` flips it off and clears any "thinking-out-loud" text, `Completed` finalises the bubble and persists the new message blocks for the next turn.

## When to use

When the chat needs to read/write app state on demand — search, lookups, mutations, structured queries. Goes beyond `chatbot-streaming` (no tools) by handling the multi-step planner→tool→synthesise loop with a single visible bubble.

## Snippet

```csharp
private async Task SendChatMessageAsync(string userMessage, Guid caseId)
{
    _chatIsProcessing.Value = true;

    try
    {
        var session = await GetOrHydrateChatSessionAsync(caseId);
        session.AppendUserMessage(userMessage);

        var assistantEntry = new ChatMessageEntry { Role = ChatMessageRole.Model };
        assistantEntry.IsProcessing.Value = true;
        _chatMessages.Add(assistantEntry);

        var responseText = new StringBuilder();
        var afterToolCall = false;
        var preRunCount = session.Messages.Count;

        await foreach (var ev in Emerge.Run<ChatResponse>(model, session.BuildKernelContext(), pass =>
        {
            pass.SystemPrompt = systemPrompt;
            pass.Command = userMessage;
            pass.Temperature = 0.3;
            pass.MaxIterations = 15;
            RegisterChatTools(pass, caseId);
        }))
        {
            switch (ev)
            {
                case ModelText<ChatResponse> text:
                    if (afterToolCall)
                    {
                        responseText.Clear();
                        afterToolCall = false;
                    }
                    responseText.Append(text.Text);
                    assistantEntry.Content.Value = responseText.ToString();
                    break;

                case ToolCallPlanned<ChatResponse>:
                    assistantEntry.IsProcessing.Value = true;
                    break;

                case ToolCallResult<ChatResponse>:
                    afterToolCall = true;
                    assistantEntry.IsProcessing.Value = false;
                    break;

                case Completed<ChatResponse> completed:
                    assistantEntry.IsProcessing.Value = false;
                    if (completed.Result != null && !string.IsNullOrEmpty(completed.Result.Response))
                        assistantEntry.Content.Value = completed.Result.Response;

                    var finalMessages = completed.Context.Messages;
                    session.Replace(finalMessages);
                    for (int i = preRunCount; i < finalMessages.Count; i++)
                        await PersistMessageBlockAsync(caseId, finalMessages[i], userId: null);
                    break;
            }
        }
    }
    finally { _chatIsProcessing.Value = false; }
}
```

## Notes

- `afterToolCall = true` after `ToolCallResult`. The next `ModelText` clears the StringBuilder — model-text emitted *before* a tool call is the LLM's planning narration; the *real* answer is whatever it says after the tool returns. Keeping both would leak "Let me check…" into the final bubble.
- `IsProcessing.Value` toggles per phase so the typing-dots spinner only renders while the model is actively working, not during tool execution gaps.
- `MaxIterations: 15` caps tool-loop runaway. Pair with a strong system prompt about confirming before mutations.
- `preRunCount` snapshots the session length before the run. After `Completed`, persist only the *new* messages (`[preRunCount..]`) so tool memory survives a page refresh.
- Tools are registered inside the `pass =>` lambda via `pass.AddTool(Tool.Of(name, description, async (a1, a2) => ...))`. Annotate lambda parameters with `[Description("...")]` to document them to the LLM.

## See also

- `chatbot-streaming` — simpler tool-less variant.
- `emergence` (top-level guide) — full event-stream taxonomy and tool registration.
