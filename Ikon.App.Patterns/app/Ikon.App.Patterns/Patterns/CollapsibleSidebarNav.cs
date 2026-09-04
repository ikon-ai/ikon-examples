namespace Ikon.App.Patterns.Patterns;

// Pattern: collapsible-sidebar-nav — see docs/patterns/collapsible-sidebar-nav.md.
// The enums, records, and collections below stand in for the app's real event/camera state that
// drives the per-item live badges.
internal sealed class CollapsibleSidebarNav : IPatternDemo
{
    public string Slug => "collapsible-sidebar-nav";
    public string Title => "Collapsible sidebar nav";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => RenderSidebar(view);

    private enum Severity { Info, Warning, Alert }

    private enum EventStatus { Open, Acknowledged, Closed }

    private sealed record Event(string Id, Severity Severity, EventStatus Status);

    private sealed record CameraStream(string Id, bool OfflineFlagged);

    private readonly ReactiveList<Event> _events = new();
    private readonly ConcurrentDictionary<string, CameraStream> _streams = new();

    #region docsnippet:pattern-collapsible-sidebar-nav
    private readonly PersistentUserReactive<bool> _sidebarCollapsed = new(false);
    private readonly ClientReactive<string> _activeSection = new(initialValue: "cameras");

    private void RenderSidebar(UIView view)
    {
        var collapsed = _sidebarCollapsed.Value;
        var widthClass = collapsed ? "w-14" : "w-56";

        var openCount = _events.Count(e => e.Severity == Severity.Alert && e.Status == EventStatus.Open);
        var offlineCount = _streams.Values.Count(s => s.OfflineFlagged);

        view.Column([$"hidden md:flex {widthClass} bg-zinc-925 border-r border-zinc-800/80 flex-shrink-0 transition-all duration-150"], content: view =>
        {
            view.Box(["flex-1 min-h-0 overflow-y-auto py-3 px-2 gap-1"], content: items =>
            {
                items.Column(["gap-0.5"], content: col =>
                {
                    RenderSidebarItem(col, "cameras", "video", "Cameras", collapsed,
                        offlineCount > 0 ? (offlineCount.ToString(), "rose") : default);
                    RenderSidebarItem(col, "events", "bell", "Events", collapsed,
                        openCount > 0 ? (openCount.ToString(), "rose") : default);
                    RenderSidebarItem(col, "insights", "bar-chart-2", "Insights", collapsed, default);
                    RenderSidebarItem(col, "settings", "settings", "Settings", collapsed, default);
                });
            });

            view.Box(["border-t border-zinc-800/80 px-2 py-2"], content: footer =>
            {
                footer.Button(
                    [$"w-full px-2 py-1.5 rounded-md hover:bg-zinc-800/60 text-zinc-500 hover:text-zinc-300 flex items-center {(collapsed ? "justify-center" : "gap-2")} text-xs"],
                    onClick: async () => _sidebarCollapsed.Value = !_sidebarCollapsed.Value,
                    content: btn =>
                    {
                        btn.Icon(["w-3.5 h-3.5"], name: collapsed ? "chevrons-right" : "chevrons-left");

                        if (!collapsed)
                        {
                            btn.Text([], "Collapse");
                        }
                    });
            });
        });
    }

    private void RenderSidebarItem(UIView view, string sectionKey, string iconName, string label, bool collapsed, (string Text, string Tone)? badge = null)
    {
        var active = _activeSection.Value == sectionKey;
        var style = active ? "bg-zinc-800 text-zinc-100" : "text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/60";

        view.Button(
            [$"relative px-2 py-2 rounded-md flex items-center {(collapsed ? "justify-center" : "gap-2.5")} text-sm font-medium {style}"],
            onClick: async () => _activeSection.Value = sectionKey,
            content: btn =>
            {
                btn.Box(["relative flex-shrink-0"], content: iconBox =>
                {
                    iconBox.Icon(["w-4 h-4"], name: iconName);

                    if (collapsed && badge is { } b)
                    {
                        var dotColor = b.Tone == "amber" ? "bg-amber-400" : "bg-rose-500";
                        iconBox.Box([$"absolute -top-0.5 -right-0.5 w-2 h-2 rounded-full ring-1 ring-zinc-925 {dotColor}"]);
                    }
                });

                if (!collapsed)
                {
                    btn.Text(["truncate flex-1"], label);

                    if (badge is { } b)
                    {
                        btn.Box(["px-1.5 py-0 rounded-full ring-1 ring-rose-500/40 bg-rose-500/15 text-xs font-semibold text-rose-300"], content: c => c.Text([], b.Text));
                    }
                }
            });
    }
    #endregion
}
