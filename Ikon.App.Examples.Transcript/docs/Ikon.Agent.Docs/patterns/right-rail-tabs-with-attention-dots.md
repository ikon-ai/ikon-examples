<!-- mined-from: Veldra.OS -->
# Right Rail Tabs With Attention Dots — Stable Container, Per-Tab Unread

A persistent right-side rail that switches between Live feed / Details / AI / Sources by tab, with a per-tab amber dot when that tab has unacknowledged content. The outer Column is stable across switches so reactive subscriptions inside each body don't churn — the earlier bug being that branch-swapping at the row level made the detail body go unresponsive after a few switches.

## When to use

Any persistent side panel with multiple modes. Especially when one tab's content (chat, alerts, sources) updates in the background while the user is parked on another tab — the dot is a low-noise way to surface new content without yanking attention.

## Snippet

```csharp
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
```

## Notes

- Keep the body container stable (always the same outer `Column`) — switching tabs only swaps the inner branch. Swapping outer containers re-creates reactive subscriptions and triggers stuck-state bugs.
- Dot only shows when the tab is NOT active (otherwise the user is already looking at it).
- Per-tab attention condition is computed inline from existing reactive state — no separate "unread" counter to maintain.
- Marker click on a feed item can `_rightTab.Value = "details"` and `_selectedDetectionId.Value = ...` to deep-link.
- These tabs stretch (`flex-1`) because they label a fixed-width rail, where equal segments read as one control. Page-level navigation is the opposite case — content-width rows on a shared rail — so do not carry this treatment into an app header.

## See also

- `nav-and-menu-rows` — the tokens for page tabs, sidebar rows and menu rows, and when equal-width is wrong.
