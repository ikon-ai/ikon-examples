<!-- mined-from: Sentinel -->
# Global Keyboard Shortcuts — Single dispatcher with Esc-stack

A single `view.KeyboardListener` mounted at the root with `global: true` listens for a fixed allowlist of keys. The handler dispatches via a `switch` on the lowercased key. The `Escape` case implements an "Esc-stack" — close the topmost open dialog/popover/expanded item first, before fall-through. Modifier keys (`Ctrl/Cmd+K` for the command palette) branch within their key case.

## When to use

Power-user dashboards where keyboard navigation is the primary interaction (security ops, agent dashboards, IDE-style tools). Use one global listener at the root rather than scattering per-component listeners — simpler to reason about, no event ordering issues, and the Esc-stack works correctly because there's a single dispatcher that knows which dialog is "topmost."

## Snippet

```csharp
view.Column(["h-screen w-full bg-zinc-950"], content: view =>
{
    view.KeyboardListener(
        global: true,
        keys: ["a", "d", "f", "j", "k", "m", "s", "i", "e", "?", "/", "Escape"],
        onKeyDown: HandleKeyDown);

    RenderTopStrip(view);
    RenderAlertBanner(view);
    // ... main UI ...
});

private async Task HandleKeyDown(KeyboardEventArgs args)
{
    var key = args.Key?.ToLowerInvariant();
    var topOpenAlert = _events.Value.FirstOrDefault(e => e.Severity == Severity.Alert && e.Status == EventStatus.Open);

    switch (key)
    {
        case "a":
            if (topOpenAlert != null) UpdateEventStatus(topOpenAlert.Id, EventStatus.Acknowledged);
            break;
        case "d":
            if (topOpenAlert != null) UpdateEventStatus(topOpenAlert.Id, EventStatus.Dismissed);
            break;
        case "m":
            _audioMuted.Value = !_audioMuted.Value;
            break;
        case "s":
            _activeSection.Value = "settings";
            break;
        case "j":
            NavigateEventList(direction: +1);
            break;
        case "k":
            if (args.MetaKey || args.CtrlKey)
            {
                _paletteQuery.Value = "";
                _paletteOpen.Value = !_paletteOpen.Value;
            }
            else
            {
                NavigateEventList(direction: -1);
            }
            break;
        case "?":
        case "/":
            _cheatSheetOpen.Value = !_cheatSheetOpen.Value;
            break;
        case "escape":
            // Esc-stack: close the topmost layer first, fall through if nothing matched
            if (_paletteOpen.Value)              _paletteOpen.Value = false;
            else if (_renamingStreamId.Value != null) _renamingStreamId.Value = null;
            else if (_addCameraOpen.Value)       _addCameraOpen.Value = false;
            else if (_triageQueueOpen.Value)     _triageQueueOpen.Value = false;
            else if (_cheatSheetOpen.Value)      _cheatSheetOpen.Value = false;
            else if (_expandedEventId.Value != null) _expandedEventId.Value = null;
            else if (_focusedStreamId.Value != null)  _focusedStreamId.Value = null;
            break;
    }
}
```

## Notes

- Pass an *explicit* `keys: [...]` allowlist — without it the listener would intercept normal typing inside text fields. The platform respects focus context, but listing the keys makes it obvious which characters are "shortcuts" and lets you grep for them.
- The Esc-stack ordering goes from "least committed" (palette, popovers) to "most committed" (selected items, focused panels). The first match wins — without `else if` chains you'd close everything at once on a single Esc press, which is jarring.
- `j / k` for next/previous is the gmail/vim convention — pair them with the same `NavigateXxx(direction)` method so the only difference is the sign.
- Modifier keys hang off `args` (`MetaKey`, `CtrlKey`, `ShiftKey`, `AltKey`). Branch *inside* the key case rather than dispatching on a "key+modifiers" string — it keeps the key allowlist clean.
- Provide a cheat-sheet dialog (`?` or `/`) that lists the shortcuts. Otherwise even your power users won't know them.

## See also

- `command-palette-jump` — `Cmd+K` opens the palette; this listener is the canonical mount point for that
- `collapsible-sidebar-nav` — the sections that `s / e / i` keys navigate to
