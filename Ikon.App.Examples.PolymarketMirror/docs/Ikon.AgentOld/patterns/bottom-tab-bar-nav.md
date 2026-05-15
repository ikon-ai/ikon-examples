<!-- mined-from: NoBrainer -->
# Bottom Tab Bar Nav — Mobile-style three-tab pages

A flex-shrink-0 bottom row with three icon+label buttons that swap the main content via a `ClientReactive<TEnum>`. The active tab gets a tinted color and a slightly bolder weight; the inactive tabs sit at low opacity. Pairs naturally with `h-screen flex flex-col` root and a `ScrollArea` middle.

## When to use

You want a phone-like home shell with 2-4 top-level destinations (Threads / Skills / Settings, Feed / Search / Profile). Choose this over `view.Tabs(...)` when the tabs should anchor the bottom of the viewport, when each tab is a full screen, and when you want bespoke iconography rather than a tab bar control.

## Snippet

```csharp
private void RenderRoot(UIView view)
{
    view.Column(["h-screen flex flex-col"], content: view =>
    {
        RenderHeader(view);

        switch (_activeTab.Value)
        {
            case NoBrainerTab.Threads: RenderThreadList(view); break;
            case NoBrainerTab.Skills:  RenderSkillsTab(view);  break;
            case NoBrainerTab.Settings:RenderSettingsTab(view);break;
        }

        RenderTabBar(view);
    });
}

private void RenderTabBar(UIView view)
{
    view.Row(["px-2 py-2 flex-shrink-0 border-t border-black/[0.04] justify-around"], content: view =>
    {
        RenderTabButton(view, NoBrainerTab.Threads, "layers", "Threads");
        RenderTabButton(view, NoBrainerTab.Skills, "sparkles", "Skills");
        RenderTabButton(view, NoBrainerTab.Settings, "settings", "Settings");
    });
}

private void RenderTabButton(UIView view, NoBrainerTab tab, string icon, string label)
{
    var isActive = _activeTab.Value == tab;
    var color = isActive ? "text-amber-800/60" : "text-black/20";
    var weight = isActive ? "font-medium" : "font-normal";

    view.Button([$"flex flex-col items-center gap-1 px-4 py-1 bg-transparent border-0 {color} hover:text-amber-800/40"],
        onClick: async () => _activeTab.Value = tab,
        content: view =>
        {
            view.Icon(["w-5 h-5"], name: icon);
            view.Text([$"text-[10px] {weight}"], label);
        });
}
```

## Notes

- The shell column MUST be `h-screen flex flex-col`; the middle scroll area must use `flex-1 min-h-0` or content will push the tab bar off-screen.
- Use a `ClientReactive<TEnum>` for the active tab so each client navigates independently.
- The header and tab bar both want `flex-shrink-0` so only the content scrolls.
- Resetting state when entering a tab (e.g. clearing the active thread on tab switch) belongs in the button's `onClick`, not in the renderer.

## See also

- `sidebar-and-content-layout` — the desktop counterpart with a left rail
- `multi-page-tabs-with-routing` — tabs that also drive the URL path
