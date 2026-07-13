<!-- mined-from: Threads -->
# Right Panel Tabs — Toggleable side panel with tab switching

A right-edge panel that opens when `_rightPanelTab.Value != null`, shows tab buttons across the top (Files / Log / Oracle / Context), and renders different content per tab. Width is resizable by dragging a handle on the left edge; the percentage is persisted in a reactive. Closing the X clears the reactive and the panel collapses, leaving the main area to take the full width.

> **Default to a fixed-width panel.** The draggable resize affordance shown here (`view.ResizeHandle(...)`) is NOT a built-in component — it needs a custom `view.AddNode("custom.resize-handle", ...)` React component that you build yourself (see the custom-component wiring in the real-time multi-user pattern: a resolver + `registerModule` in `app.tsx` + the C# `AddNode` extension). There is no ready-made one to copy, and without it the build fails with CS1061 on `view.ResizeHandle`. Unless the resize handle is specifically required, replace `view.ResizeHandle(...)` with a plain CSS-width Box and a fixed panel width — that needs no app-local code.

## When to use

Apps with a primary work area where supporting context (build logs, file artifacts, model log, debug info) should be inspectable on demand without leaving the main flow. Use this rather than a modal dialog for content the user wants to *keep glancing at* while doing the main task. The toggle from null → tab name → null gives "closed" / "open with this tab" with a single reactive.

## Snippet

```csharp
private readonly ClientReactive<string?> _rightPanelTab = new(initialValue: (string?)null);
private readonly ClientReactive<int> _rightPanelWidth = new(initialValue: 35);

view.Row(["flex-1 overflow-hidden"], content: view =>
{
    view.Column(["flex-1 overflow-hidden bg-background min-w-0"], content: view =>
    {
        RenderMessages(view);
        RenderInput(view, thread);
    });

    if (_rightPanelTab.Value != null)
    {
        view.ResizeHandle(side: "right",
            onResized: async result => _rightPanelWidth.Value = Math.Clamp(result.Percent, 25, 75));

        view.Column(
            ["flex-shrink-0 h-full bg-card overflow-hidden border-l border-border",
             $"w-[{_rightPanelWidth.Value}%]"], content: view =>
        {
            RenderRightPanelTabs(view, thread);
            RenderRightPanelContent(view, thread);
        });
    }
});

private void RenderRightPanelTabs(UIView view, ThreadInfo thread)
{
    view.Row(["px-2 py-1 border-b border-border gap-1 flex-shrink-0 bg-card/80"], content: view =>
    {
        void Tab(string id, string label, string icon)
        {
            var isActive = _rightPanelTab.Value == id;
            view.Button([
                    "h-7 px-2.5 rounded-md text-xs font-medium flex items-center gap-1.5",
                    isActive ? "bg-primary/15 text-primary" : "text-muted-foreground hover:text-foreground hover:bg-muted/50"],
                onClick: async () => _rightPanelTab.Value = id,
                content: v =>
                {
                    v.Icon(["w-3.5 h-3.5"], name: icon);
                    v.Text(null, label);
                });
        }

        Tab("files", "Files", "file-code");
        Tab("log", "Log", "activity");
        Tab("context", "Context", "radar");

        view.Box(["flex-1"]);

        view.Button(["h-7 w-7 p-0 rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/50"],
            onClick: async () => _rightPanelTab.Value = null,
            content: v => v.Icon(["w-3.5 h-3.5"], name: "x"));
    });
}

private void RenderRightPanelContent(UIView view, ThreadInfo thread)
{
    view.Box(["flex-1 overflow-hidden"], content: view =>
    {
        switch (_rightPanelTab.Value)
        {
            case "files":   RenderArtifactPanel(view, thread); break;
            case "log":     RenderLogPanel(view); break;
            case "context": RenderContextRadar(view); break;
        }
    });
}
```

## Notes

- A single `ClientReactive<string?>` for the tab covers both the "is it open" question (null vs not) and "which tab" (specific id) — no separate `_panelOpen` flag is needed.
- Width is `_rightPanelWidth.Value` (an int 25-75) rather than fractional; integer percent strings make tailwind happy and clamp logic obvious. Use `Math.Clamp(result.Percent, 25, 75)` to enforce min/max during resize.
- The X button setting `_rightPanelTab.Value = null` is the canonical close gesture; users expect that and `Esc` to close. Wire `Esc` via a `KeyboardListener` if you support it.
- Each tab's content is its own private renderer — keep `RenderRightPanelContent` as a tiny dispatcher to avoid the big nested switch swallowing the layout shape.
- For a phone-size viewport, the same reactive can drive a full-screen overlay instead of a side panel — branch on viewport width in the parent layout.

## See also

- `three-pane-desktop-layout` — the broader layout this panel slots into
- `command-palette-jump` — the keyboard-first counterpart to this panel's tab buttons
