namespace Ikon.App.Patterns.Patterns;

// Pattern: right-rail-tabs-with-attention-dots — see docs/patterns/right-rail-tabs-with-attention-dots.md.
// The docsnippet region keeps a stable outer container and swaps only the inner branch; the stubs
// outside it stand in for the per-tab state and the four tab bodies the caller supplies.
internal sealed class RightRailTabsWithAttentionDots : IPatternDemo
{
    public string Slug => "right-rail-tabs-with-attention-dots";
    public string Title => "Right-rail tabs with attention dots";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Navigation pattern: a right-rail tab strip with attention dots that keeps a stable outer container and swaps only the inner body branch. The four tab bodies are stubs here — see the source and docs/patterns/right-rail-tabs-with-attention-dots.md.");

    private enum AiChatEntryKind { Reply, Proactive }

    private sealed record Alert(string Id, bool Acknowledged, DateTimeOffset Timestamp);
    private sealed record AiChatEntry(string Id, AiChatEntryKind Kind, DateTime TimeUtc);

    private readonly ClientReactive<string> _rightTab = new("feed");
    private readonly ReactiveList<Alert> _alerts = new();
    private readonly ReactiveList<AiChatEntry> _aiChatEntries = new();

    private void RenderFeedTabBody(UIView view) => throw new NotImplementedException();
    private void RenderDetailTabBody(UIView view) => throw new NotImplementedException();
    private void RenderAiChatTabBody(UIView view) => throw new NotImplementedException();
    private void RenderSourcesTabBody(UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-right-rail-tabs-with-attention-dots
    private void RenderRightRail(UIView view)
    {
        view.Column(["w-[360px] h-full min-h-0 shrink-0 border-l"], content: view =>
        {
            view.Row(["shrink-0 border-b"], content: view =>
            {
                RenderTabButton(view, "feed", "Live feed");
                RenderTabButton(view, "details", "Details");
                RenderTabButton(view, "chat", "AI");
                RenderTabButton(view, "sources", "Sources");
            });
            view.Column(["flex-1 min-h-0"], content: view =>
            {
                switch (_rightTab.Value)
                {
                    case "details": RenderDetailTabBody(view); break;
                    case "chat":    RenderAiChatTabBody(view); break;
                    case "sources": RenderSourcesTabBody(view); break;
                    default:        RenderFeedTabBody(view); break;
                }
            });
        });
    }

    private void RenderTabButton(UIView view, string tabId, string label)
    {
        bool active = _rightTab.Value == tabId;
        bool hasAttention = tabId switch
        {
            "feed" => _alerts.Value.Any(a => !a.Acknowledged
                && a.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-5)),
            "chat" => _aiChatEntries.Value.Any(e => e.Kind == AiChatEntryKind.Proactive
                && e.TimeUtc > DateTime.UtcNow.AddMinutes(-2)),
            _ => false,
        };
        view.Box([
            "flex-1 px-3 py-2.5 cursor-pointer items-center justify-center relative",
            active ? "bg-[#121826] border-b-2 border-amber-500" : "border-b-2 border-transparent",
        ], onClick: async () => { _rightTab.Value = tabId; await Task.CompletedTask; },
        content: view =>
        {
            view.Text([active ? "text-white" : "text-slate-400", "text-[11px] uppercase font-semibold"], label);

            if (hasAttention && !active)
            {
                view.Box(["absolute top-1.5 right-1.5 w-1.5 h-1.5 rounded-full bg-amber-500"]);
            }
        });
    }
    #endregion
}
