namespace Ikon.App.Patterns.Patterns;

// Pattern: clickable-reference-card-in-chat — see docs/patterns/clickable-reference-card-in-chat.md.
// The stubs outside the region stand in for the dialog state, the badge/icon helpers and the entity
// lookup the app owns; the example region is the canonical body the doc extracts.
internal sealed class ClickableReferenceCardInChat : IPatternDemo
{
    public string Slug => "clickable-reference-card-in-chat";
    public string Title => "Clickable reference card in chat";
    public string Category => "Chat";
    // The model's tool call is what fills the cards in the app; here two are seeded as if it had
    // referred a person and a file, and the detail dialog is reduced to a line naming the selection.
    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Box(["ml-auto max-w-[80%] rounded-lg bg-foreground/10 px-4 py-2"], content: bubble =>
                bubble.Text([Text.Body], "Who signed off on the inspection report?"));

            foreach (var message in _sampleMessages)
            {
                RenderChatMessage(col, message);
            }

            col.Text([Text.Caption], _showEntityDetailDialog.Value && _sampleMessages.FirstOrDefault(m => m.EntityReferenceId.Value == _selectedEntityId.Value) is { } selected
                ? $"Detail dialog open for {selected.EntityReferenceName.Value}"
                : "Press a card to open its detail");
        });
    }

    public enum ChatMessageRole { User, Assistant }
    public enum EntityType { Person, File, Place, Unknown }

    private readonly Reactive<Guid?> _selectedEntityId = new(null);
    private readonly Reactive<bool> _showEntityDetailDialog = new(false);
    private readonly string caseId = "";

    private readonly ChatMessageEntry[] _sampleMessages = [SampleReference("Maria Lindqvist", EntityType.Person, 0.94), SampleReference("inspection-2026-09.pdf", EntityType.File, 0.81)];

    private sealed record ChatAnswer(string Message);

    private static ChatMessageEntry SampleReference(string name, EntityType type, double confidence)
    {
        var entry = new ChatMessageEntry { Role = ChatMessageRole.Assistant };
        entry.EntityReferenceId.Value = Guid.NewGuid();
        entry.EntityReferenceName.Value = name;
        entry.EntityReferenceType.Value = type.ToString();
        entry.EntityReferenceConfidence.Value = confidence;
        return entry;
    }

    private string T(string key) => key;

    private static string[] GetEntityTypeBadgeStyle(EntityType entityType) => entityType switch
    {
        EntityType.Person => ["bg-sky-500/10"],
        EntityType.File => ["bg-amber-500/10"],
        _ => ["bg-card"],
    };

    private static string GetEntityTypeIcon(EntityType entityType) => entityType switch
    {
        EntityType.Person => "user",
        EntityType.File => "file-text",
        EntityType.Place => "map-pin",
        _ => "circle-help",
    };

    private static string GetConfidenceLabel(double confidence) => confidence >= 0.9 ? "High confidence" : "Likely match";

    private Task<string> ReferEntitiesAsync(string caseId, string[] entityNames) => throw new NotImplementedException();

    #region example:pattern-clickable-reference-card-in-chat
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
        // A filled EntityReferenceId flips the entry into card mode; otherwise it renders as a normal bubble.
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
                text: message.EntityReferenceName.Value ?? "",
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
                                {
                                    meta.Text(["text-xs opacity-75"],
                                        T(GetConfidenceLabel(message.EntityReferenceConfidence.Value.Value)));
                                }
                            });
                        });
                        row.Icon([Icon.Default, "w-4 h-4 opacity-50"], name: "chevron-right");
                    });
                });
        });
    }

    // Call from the Emerge.Run configure callback: it runs before every iteration, but tools
    // carry over between iterations and AddTool skips a name already on the pass, so the repeat is harmless.
    private void RegisterReferTool(EmergePass<ChatAnswer> pass)
    {
        pass.AddTool(Tool.Of("refer_entities",
            "Display interactive entity reference cards in the chat for one or more entities. " +
            "Use this when discussing specific entities to let the user click through to their full details.",
            async (string[] entityNames) => await ReferEntitiesAsync(caseId, entityNames)));
    }
    #endregion
}
