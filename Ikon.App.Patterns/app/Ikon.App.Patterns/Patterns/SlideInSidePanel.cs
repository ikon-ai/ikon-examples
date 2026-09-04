namespace Ikon.App.Patterns.Patterns;

// Pattern: slide-in-side-panel — see docs/patterns/slide-in-side-panel.md.
// The four throwing stubs stand in for the real tab bodies the drawer switches between.
internal sealed class SlideInSidePanel : IPatternDemo
{
    public string Slug => "slide-in-side-panel";
    public string Title => "Slide-in side panel";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => RenderSystemPanel(view);

    private void RenderSystemCharacter(UIView view) => throw new NotImplementedException();
    private void RenderSystemInventory(UIView view) => throw new NotImplementedException();
    private void RenderSystemQuests(UIView view) => throw new NotImplementedException();
    private void RenderSystemLog(UIView view) => throw new NotImplementedException();

    #region docsnippet:pattern-slide-in-side-panel
    private readonly ClientReactive<bool> _systemPanelOpen = new(false);
    private readonly ClientReactive<string> _systemTab = new("character");

    private void RenderSystemPanel(UIView view)
    {
        if (!_systemPanelOpen.Value)
        {
            return;
        }

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
                        text: "✕",
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
                            text: tab.Item2,
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
    #endregion
}
