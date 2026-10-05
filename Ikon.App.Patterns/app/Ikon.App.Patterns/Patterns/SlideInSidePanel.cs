namespace Ikon.App.Patterns.Patterns;

// Pattern: slide-in-side-panel — see docs/patterns/slide-in-side-panel.md.
// The four small tab bodies outside the region stand in for the game's real character sheet,
// inventory, quest list and log; the demo frame gives the absolutely positioned drawer a stage.
internal sealed class SlideInSidePanel : IPatternDemo
{
    public string Slug => "slide-in-side-panel";
    public string Title => "Slide-in side panel";
    public string Category => "Navigation";

    public void RenderDemo(IView view)
    {
        view.Box(["relative h-[520px] w-full max-w-3xl overflow-hidden rounded-xl border border-white/10 bg-[#161412]"], content: stage =>
        {
            stage.Column(["p-6 gap-3 items-start"], content: col =>
            {
                col.Text(["text-sm text-[#A8A29E]"], "The Ashen Road · Day 3 · Evening");
                col.Button([Button.OutlineSm], text: "Open System panel",
                    onClick: async () => { _systemPanelOpen.Value = true; });
            });

            RenderSystemPanel(stage);
        });
    }

    private static void RenderSystemRows(UIView view, IEnumerable<(string Label, string Value)> rows)
    {
        view.Column(["gap-2"], content: list =>
        {
            foreach (var (label, value) in rows)
            {
                list.Row(["justify-between text-sm border-b border-white/5 py-1.5"], content: row =>
                {
                    row.Text(["text-[#A8A29E]"], label);
                    row.Text(["text-[#EDE7DC]"], value);
                });
            }
        });
    }

    private void RenderSystemCharacter(UIView view) => RenderSystemRows(view,
        [("Name", "Maren Holt"), ("Class", "Wayfinder"), ("Level", "7"), ("Health", "42 / 50"), ("Stamina", "18 / 30"), ("Gold", "134")]);

    private void RenderSystemInventory(UIView view) => RenderSystemRows(view,
        [("Iron shortsword", "equipped"), ("Travel cloak", "equipped"), ("Healing draught", "× 3"), ("Rope, 15 m", "× 1"), ("Old map fragment", "quest")]);

    private void RenderSystemQuests(UIView view) => RenderSystemRows(view,
        [("The Drowned Bell", "active"), ("Tolls for the Ferryman", "active"), ("A Debt in Brackwater", "done")]);

    private void RenderSystemLog(UIView view) => RenderSystemRows(view,
        [("18:42", "Reached the Ashen Road"), ("17:10", "Bought 2 healing draughts"), ("15:55", "Ferryman agreed to wait"), ("12:03", "Found an old map fragment")]);

    #region example:pattern-slide-in-side-panel
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
