<!-- mined-from: Vienola -->
# Slide-In Side Panel — Backdrop + Animated Drawer With Tabbed Body

A left-side drawer that animates in from off-screen when toggled, with a clickable backdrop that dismisses, and tabbed scrollable content inside. Uses `motion-[0:translate-x-[-100%],100:translate-x-0] motion-duration-300ms` for the slide. The header (title + tabs) is fixed; the content area scrolls.

## When to use

System menus, settings, character sheets, inspectors — secondary content that overlays the main view rather than reflowing it. Mobile-first: better than docking a sidebar at narrow widths.

## Snippet

```csharp
private readonly ClientReactive<bool> _systemPanelOpen = new(false);
private readonly ClientReactive<string> _systemTab = new("character");

private void RenderSystemPanel(UIView view)
{
    if (!_systemPanelOpen.Value) return;

    // Backdrop
    view.Box(["absolute inset-0 bg-black/30 pointer-events-auto"],
        content: _ => { },
        onClick: async () => { _systemPanelOpen.Value = false; });

    // Animated panel
    view.Box([
        "absolute left-0 top-0 bottom-0 w-[360px] pointer-events-auto overflow-hidden flex flex-col",
        "bg-[#0E0E0E] border-r border-white/10",
        "motion-[0:translate-x-[-100%],100:translate-x-0] motion-duration-300ms motion-ease-ease-out motion-fill-both"
    ], content: view =>
    {
        // Fixed header — title + close + tabs
        view.Column(["p-5 pb-2 gap-3 flex-shrink-0"], content: header =>
        {
            header.Row(["items-center justify-between"], content: row =>
            {
                row.Text(["text-lg font-semibold text-[#D6A85C]"], "System");
                row.Button([Button.GhostSm, "!px-2 !py-1"],
                    label: "✕",
                    onClick: async () => { _systemPanelOpen.Value = false; });
            });

            header.Row(["gap-1 bg-white/5 rounded-lg p-1 flex-wrap"], content: row =>
            {
                foreach (var tab in new[] { ("character", "Character"), ("inventory", "Items"), ("quests", "Quests"), ("log", "Log") })
                {
                    bool active = _systemTab.Value == tab.Item1;
                    string style = active
                        ? "text-xs font-semibold text-[#EDE7DC] bg-white/10 rounded-md px-3 py-1.5 cursor-pointer transition-all"
                        : "text-xs text-[#A8A29E] px-3 py-1.5 cursor-pointer hover:text-[#EDE7DC] transition-all";
                    row.Button([style, "border-none"],
                        label: tab.Item2,
                        onClick: async () => { _systemTab.Value = tab.Item1; });
                }
            });
        });

        // Scrollable tab content
        view.ScrollArea(
            scrollbars: ScrollAreaScrollbars.Vertical,
            type: ScrollAreaType.Auto,
            rootStyle: ["flex-1 min-h-0"],
            viewportStyle: ["px-5 pb-5"],
            content: sv =>
            {
                switch (_systemTab.Value)
                {
                    case "character": RenderSystemCharacter(sv); break;
                    case "inventory": RenderSystemInventory(sv); break;
                    case "quests": RenderSystemQuests(sv); break;
                    case "log": RenderSystemLog(sv); break;
                }
            });
    });
}
```

## Notes

- The whole panel only renders when `_systemPanelOpen.Value == true` — no need to keep DOM around when hidden because the slide-in animation runs on first render via `motion-fill-both`.
- Backdrop is `absolute inset-0 bg-black/30` (NOT `fixed` — match the parent's positioning context).
- `motion-fill-both` keeps the final state after the keyframes complete; without it the panel snaps back.
- Use `flex-shrink-0` on the header and `flex-1 min-h-0` on the scroll area — otherwise the scroll area collapses to its content height.
- Tab buttons aren't `Tabs` component because we want pill-style highlighting + tighter control over text colors; switch + button list is fine.

## See also

- `bottom-tab-bar-nav` — different shape: persistent bottom nav, not a drawer
- `collapsible-sidebar-nav` — different shape: docks beside content rather than overlaying
