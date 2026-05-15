<!-- mined-from: Sentinel -->
# Collapsible Sidebar Nav — Icon rail with badges

A vertical left rail that toggles between a wide labeled mode (`w-56`) and a narrow icon-only mode (`w-14`). Each item shows an icon, label, and optional count/dot badge driven by reactive state. The collapsed state lives in `PersistentUserReactive<bool>` so it sticks across sessions for that user.

## When to use

Desktop app shells with 4-7 top-level sections (Cameras / Events / Insights / Settings / Billing). Pair with a separate `RenderMobileTabBar` for narrow viewports — the sidebar uses `hidden md:flex`, the tab bar uses `md:hidden`. Use this rather than `view.Tabs` when you want bespoke iconography, persistent collapse, and per-item live badges.

## Snippet

```csharp
private readonly PersistentUserReactive<bool> _sidebarCollapsed = new(false);
private readonly ClientReactive<string> _activeSection = new(initialValue: "cameras");

private void RenderSidebar(UIView view)
{
    var collapsed = _sidebarCollapsed.Value;
    var widthClass = collapsed ? "w-14" : "w-56";

    var openCount = _events.Value.Count(e => e.Severity == Severity.Alert && e.Status == EventStatus.Open);
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
                    if (!collapsed) btn.Text([], "Collapse");
                });
        });
    });
}

private void RenderSidebarItem(UIView view, string sectionKey, string iconName, string label, bool collapsed, (string Text, string Tone)? badge = null)
{
    var active = _activeSection.Value == sectionKey;
    var keyCaptured = sectionKey;
    var style = active ? "bg-zinc-800 text-zinc-100" : "text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/60";

    view.Button(
        [$"relative px-2 py-2 rounded-md flex items-center {(collapsed ? "justify-center" : "gap-2.5")} text-sm font-medium {style}"],
        onClick: async () => _activeSection.Value = keyCaptured,
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
```

## Notes

- Collapsed mode hides the badge text and shows a colored dot in the corner of the icon — the count is preserved as signal even with no room for digits.
- The item-level `keyCaptured = sectionKey` capture is required because `sectionKey` is a parameter that gets reused if the method is called inline in a loop without this binding pattern.
- Use `PersistentUserReactive<bool>` (per-user) for the collapsed flag so each operator's preference sticks across sessions; use `ClientReactive<string>` (per-client tab) for the active section so two browser tabs don't fight over which section is open.
- The badge dot in collapsed mode uses `ring-1 ring-zinc-925` to cut a hole in itself so it visually floats above the icon.

## See also

- `bottom-tab-bar-nav` — mobile-tab-bar counterpart, often paired with this on the same page via responsive `hidden md:flex` / `md:hidden`
- `command-palette-jump` — keyboard-driven navigation across the same sections
