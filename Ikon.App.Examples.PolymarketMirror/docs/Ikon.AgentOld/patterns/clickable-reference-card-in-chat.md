<!-- mined-from: Sentrix -->
# Clickable Reference Card in Chat — LLM Tool Emits a UI Element

When the LLM mentions a specific entity (a person, file, place, recommendation), instead of letting it inline the name as text, give it a `refer_*` tool that emits a structured "reference card" message. The chat renderer detects the special message shape and renders it as a button that opens the entity's detail dialog.

## When to use

Any chat where users frequently want to dig deeper on an LLM mention. Replaces "Open the Files tab and find Smith.pdf" with a clickable card. Especially useful when entities have rich detail views you've already built (forensic profiles, file metadata, member cards).

## Snippet

```csharp
internal sealed class ChatMessageEntry
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public ChatMessageRole Role { get; init; }
    public Reactive<string> Content { get; } = new("");
    public Reactive<Guid?> EntityReferenceId { get; } = new(null);
    public Reactive<string?> EntityReferenceName { get; } = new(null);
    public Reactive<string?> EntityReferenceType { get; } = new(null);
    public Reactive<double?> EntityReferenceConfidence { get; } = new(null);
}

private void RenderChatMessage(UIView view, ChatMessageEntry message)
{
    if (message.EntityReferenceId.Value.HasValue)
    {
        RenderEntityReferenceCard(view, message);
        return;
    }
    // ... normal bubble rendering ...
}

private void RenderEntityReferenceCard(UIView view, ChatMessageEntry message)
{
    var entityType = message.EntityReferenceType.Value ?? "Unknown";
    Enum.TryParse<EntityType>(entityType, true, out var parsedType);

    view.Box(["mr-auto max-w-[80%]"], content: wrapper =>
    {
        wrapper.Button(
            [.. GetEntityTypeBadgeStyle(parsedType),
                "w-full text-left border border-secondary rounded-lg px-4 py-3 cursor-pointer hover:bg-accent/50 transition-colors"],
            label: message.EntityReferenceName.Value ?? "",
            onClick: async () =>
            {
                _selectedEntityId.Value = message.EntityReferenceId.Value;
                _showEntityDetailDialog.Value = true;
            },
            content: card =>
            {
                card.Row(["flex items-center gap-2"], content: row =>
                {
                    row.Icon([Icon.Default, "w-4 h-4"], name: GetEntityTypeIcon(parsedType));
                    row.Column(["flex-1"], content: col =>
                    {
                        col.Text(["text-sm font-medium"], message.EntityReferenceName.Value ?? "");
                        col.Row(["flex items-center gap-2"], content: meta =>
                        {
                            meta.Text(["text-xs opacity-75"], entityType);
                            if (message.EntityReferenceConfidence.Value.HasValue)
                                meta.Text(["text-xs opacity-75"],
                                    T(GetConfidenceLabel(message.EntityReferenceConfidence.Value.Value)));
                        });
                    });
                    row.Icon([Icon.Default, "w-4 h-4 opacity-50"], name: "chevron-right");
                });
            });
    });
}

// In RegisterChatTools:
pass.AddTool<ChatResponse, string[], EmergeResult>(
    "refer_entities",
    "Display interactive entity reference cards in the chat for one or more entities. " +
    "Use this when discussing specific entities to let the user click through to their full details.",
    async (string[] entityNames) => await ReferEntitiesAsync(caseId, entityNames));
```

## Notes

- The chat message is a discriminated union by convention: `EntityReferenceId.HasValue` flips the renderer into card mode, otherwise it's a normal bubble. One field on the entry doubles as both the discriminator and the payload.
- `ReferEntitiesAsync` looks up each name in the DB and appends one `ChatMessageEntry` per hit (with `EntityReferenceId` filled in) to `_chatMessages.Value`. The LLM gets a small confirmation string back so it knows the cards rendered.
- The system prompt should explicitly tell the LLM: "When mentioning specific entities by name, call refer_entities once with all names. Do not regenerate the answer afterwards." Otherwise the LLM will still inline the name redundantly.
- Card click fires the same `_selectedEntityId.Value = ...; _showEntityDetailDialog.Value = true` you'd use from a list — one detail dialog, multiple entry points.
- Style hint: `mr-auto max-w-[80%]` aligns left like an assistant bubble; the border + chevron-right is the affordance that says "click me".

## See also

- `chat-with-tool-calls` — the streaming-chat host this card-tool plugs into.
- `expandable-detail-card` — the dialog that opens when the card is clicked.
