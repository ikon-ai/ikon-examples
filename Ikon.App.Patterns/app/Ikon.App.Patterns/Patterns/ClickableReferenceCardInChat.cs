namespace Ikon.App.Patterns.Patterns;

// Pattern: clickable-reference-card-in-chat — see docs/patterns/clickable-reference-card-in-chat.md.
// The stubs outside the region stand in for the dialog state, the badge/icon helpers and the entity
// lookup the app owns; the docsnippet region is the canonical body the doc extracts.
internal sealed class ClickableReferenceCardInChat : IPatternDemo
{
    public string Slug => "clickable-reference-card-in-chat";
    public string Title => "Clickable reference card in chat";
    public string Category => "Chat";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Renders an entity-reference chat entry as a clickable card that opens a detail dialog, with an LLM tool that emits the cards. See the source and docs/patterns/clickable-reference-card-in-chat.md.");

    public enum ChatMessageRole { User, Assistant }
    public enum EntityType { Person, File, Place, Unknown }

    private readonly Reactive<Guid?> _selectedEntityId = new(null);
    private readonly Reactive<bool> _showEntityDetailDialog = new(false);
    private readonly string caseId = "";

    private sealed record ChatAnswer(string Message);

    private readonly EmergePass<ChatAnswer> pass = null!;

    private string T(string key) => throw new NotImplementedException();

    private static string[] GetEntityTypeBadgeStyle(EntityType entityType) => throw new NotImplementedException();

    private static string GetEntityTypeIcon(EntityType entityType) => throw new NotImplementedException();

    private static string GetConfidenceLabel(double confidence) => throw new NotImplementedException();

    private Task<string> ReferEntitiesAsync(string caseId, string[] entityNames) => throw new NotImplementedException();

    #region docsnippet:pattern-clickable-reference-card-in-chat
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

    private void RegisterReferTool()
    {
        pass.AddTool(Tool.Of("refer_entities",
            "Display interactive entity reference cards in the chat for one or more entities. " +
            "Use this when discussing specific entities to let the user click through to their full details.",
            async (string[] entityNames) => await ReferEntitiesAsync(caseId, entityNames)));
    }
    #endregion
}
