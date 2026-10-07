namespace Ikon.App.Patterns.Patterns;

// Pattern: right-rail-tabs-with-attention-dots — see docs/patterns/right-rail-tabs-with-attention-dots.md.
// The example region keeps a stable outer container and swaps only the inner branch; the code
// outside it stands in for the per-tab state and the four tab bodies the caller supplies, and its
// two buttons raise the events that light a dot. The rail opens on Details so a dot on Live feed
// or AI can show.
internal sealed class RightRailTabsWithAttentionDots : IPatternDemo
{
    public string Slug => "right-rail-tabs-with-attention-dots";
    public string Title => "Right-rail tabs with attention dots";
    public string Category => "Navigation";
    public void RenderDemo(IView view)
    {
        view.Row(["gap-2 flex-wrap mb-3"], content: row =>
        {
            row.Button([Button.OutlineSm], text: "Raise an alert", onClick: () =>
                _alerts.Add(new Alert(Guid.NewGuid().ToString("N"), false, DateTimeOffset.UtcNow)));
            row.Button([Button.OutlineSm], text: "AI speaks up", onClick: () =>
                _aiChatEntries.Add(new AiChatEntry(Guid.NewGuid().ToString("N"), AiChatEntryKind.Proactive, DateTime.UtcNow)));
        });

        view.Row(["h-[360px] w-fit rounded-lg overflow-hidden bg-[#0b0f17]"], content: row => RenderRightRail(row));
    }

    private enum AiChatEntryKind { Reply, Proactive }

    private sealed record Alert(string Id, bool Acknowledged, DateTimeOffset Timestamp);
    private sealed record AiChatEntry(string Id, AiChatEntryKind Kind, DateTime TimeUtc);

    private readonly ClientReactive<string> _rightTab = new("details");
    private readonly ReactiveList<Alert> _alerts = new();
    private readonly ReactiveList<AiChatEntry> _aiChatEntries = new();

    private void RenderFeedTabBody(UIView view)
    {
        view.Column(["p-3 gap-2"], content: body =>
        {
            var open = _alerts.Value.Count(a => !a.Acknowledged);
            body.Text(["text-sm text-slate-300"], open == 0 ? "No open alerts" : $"{open} open alert(s)");

            if (open > 0)
            {
                body.Button([Button.OutlineSm, "self-start"], text: "Acknowledge all",
                    onClick: () => _alerts.Update(list => list.Select(a => a with { Acknowledged = true })));
            }
        });
    }

    private void RenderDetailTabBody(UIView view) =>
        view.Text(["p-3 text-sm text-slate-300"], "Details of the selected item. Raise an alert or let the AI speak up, and a dot appears on that tab while you are on another one.");

    private void RenderAiChatTabBody(UIView view) =>
        view.Text(["p-3 text-sm text-slate-300"], $"{_aiChatEntries.Count} AI message(s)");

    private void RenderSourcesTabBody(UIView view) =>
        view.Text(["p-3 text-sm text-slate-300"], "Sources the answers cite.");

    #region example:pattern-right-rail-tabs-with-attention-dots
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
