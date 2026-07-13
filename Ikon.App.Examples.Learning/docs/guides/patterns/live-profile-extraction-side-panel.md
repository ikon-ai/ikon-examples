<!-- mined-from: Ikon.App.AutoCRM.Agent -->
# Live Profile Side Panel — Chat on the Left, Structured State on the Right

Run a background `Claude45Haiku` extraction after every chat turn. Merge only non-empty fields into a `Reactive<CustomerProfile>`. The side panel re-renders automatically and the user watches the structured CRM record fill in as the conversation proceeds.

## When to use

A chat that's collecting structured data (sales qualification, intake form, support triage). You want the user to see the collected fields update in real time without making them fill a form, and you want a compact "deal stage" rollup beside the conversation.

## Snippet

```csharp
private async Task ExtractProfileAsync()
{
    var recent = _messages.TakeLast(6)
        .Select(m => $"{(m.Role == "user" ? "Customer" : "Agent")}: {m.Content}")
        .ToList();
    if (recent.Count == 0) { return; }

    var current = _profile.Value;
    var conversation = string.Join("\n", recent);

    var result = await Emerge.Run<ProfileExtraction>(
        LLMModel.Claude45Haiku, pass =>
        {
            pass.SystemPrompt = "Extract customer info. Return only fields that are mentioned. " +
                                "Empty strings for unknown fields.";
            pass.Command = $"""
                Current profile:
                {JsonSerializer.Serialize(current)}

                Recent conversation:
                {conversation}

                Return JSON:
                {pass.JsonSchema}
                """;
            pass.Temperature = 0;
        });

    var p = _profile.Value;
    if (!string.IsNullOrEmpty(result.Name)) { p.Name = result.Name; }
    if (!string.IsNullOrEmpty(result.Budget)) { p.Budget = result.Budget; }
    if (!string.IsNullOrEmpty(result.Stage)) { p.Stage = result.Stage; }
    // ... merge each field only if non-empty
    _profile.NotifyUpdate();
}

// Fired after each Completed event:
_ = ExtractProfileAsync();
```

## Notes

- Fire-and-forget (`_ = ExtractProfileAsync()`) — extraction must never block the main chat reply.
- Always merge defensively: only overwrite when the new value is non-empty. Empty extraction means "the model didn't see new info", not "field is now empty".
- `Temperature = 0` — extraction must be deterministic.
- Render the panel as a fixed-width column on the right; show "—" for empty fields so the user can see what's still missing.

## See also

- `parallel-extract-and-reply`
- `chat-with-tool-calls`
