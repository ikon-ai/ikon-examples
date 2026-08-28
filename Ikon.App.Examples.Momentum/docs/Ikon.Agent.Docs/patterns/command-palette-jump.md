<!-- mined-from: Sentinel -->
# Command Palette — ⌘K jump and action menu

A modal dialog with a search field and a grouped, filtered list of jumpable items. Cmd-K opens it, Esc closes it, Enter activates the first match. Items are records with a label, hint, group name, icon, and a `System.Action` that runs when picked. Built fresh on every render from current app state so jumping to "Add camera" or a specific live camera Just Works.

## When to use

Apps with more than ~6 navigable destinations or repeatable actions where keyboard speed matters — security ops, agent dashboards, anything power users live in. This is the keyboard-driven counterpart to `collapsible-sidebar-nav`. Avoid for kid-friendly or one-section apps where it adds noise.

## Snippet

```csharp
private readonly ClientReactive<bool> _paletteOpen = new(initialValue: false);
private readonly ClientReactive<string> _paletteQuery = new(initialValue: "");

private record PaletteItem(string Group, string Label, string Icon, string Hint, System.Action OnSelect);

private List<PaletteItem> BuildPaletteItems()
{
    var items = new List<PaletteItem>
    {
        new("Section", "Cameras", "video", "Live grid", () => _activeSection.Value = "cameras"),
        new("Section", "Events", "bell", "Triage queue", () => _activeSection.Value = "events"),
        new("Action", "Add camera", "plus", "QR + RTSP + IP push", () => _addCameraOpen.Value = true),
        new("Action", _audioMuted.Value ? "Unmute audio" : "Mute audio", "volume-2", "Alert beeps",
            () => _audioMuted.Value = !_audioMuted.Value),
    };

    foreach (var s in _streams.Values.OrderBy(s => s.CameraLabel))
    {
        var sid = s.StreamId;
        items.Add(new("Camera", s.CameraLabel, "video", s.CameraLabel, () =>
        {
            _activeSection.Value = "cameras";
            _focusedStreamId.Value = sid;
        }));
    }

    return items;
}

private void RenderCommandPalette(UIView view)
{
    view.Dialog(
        open: _paletteOpen.Value,
        modal: true,
        onOpenChange: async open => _paletteOpen.Value = open,
        overlayStyle: ["fixed inset-0 z-[55] bg-black/60 backdrop-blur-sm"],
        contentStyle: ["fixed top-[18%] left-1/2 -translate-x-1/2 z-[56] w-[560px] max-w-[94vw] bg-zinc-950 ring-1 ring-zinc-800 rounded-lg shadow-2xl"],
        contentSlot: dview =>
        {
            var query = (_paletteQuery.Value ?? "").Trim();
            var items = BuildPaletteItems();
            var matches = string.IsNullOrEmpty(query) ? items : items
                .Where(it => it.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                          || it.Hint.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            dview.Row(["px-4 py-3 items-center gap-2"], content: row =>
            {
                row.Icon(["w-4 h-4 text-zinc-500"], name: "search");
                row.TextField(["bg-transparent flex-1 text-sm text-zinc-100 outline-none"],
                    placeholder: "Jump to camera, section, or action…",
                    value: _paletteQuery.Value,
                    onValueChange: async v => _paletteQuery.Value = v ?? "",
                    onSubmit: async _ =>
                    {
                        if (matches.Count > 0)
                        {
                            matches[0].OnSelect();
                            _paletteOpen.Value = false;
                        }
                    });
            });

            string? lastGroup = null;
            for (var i = 0; i < matches.Count && i < 40; i++)
            {
                var item = matches[i];

                if (item.Group != lastGroup)
                {
                    dview.Text(["px-4 pt-2 pb-1 text-xs text-zinc-500 font-medium"], item.Group);
                    lastGroup = item.Group;
                }

                var captured = item;
                dview.Button([$"w-full px-4 py-2 flex items-center gap-3 hover:bg-zinc-900/60"],
                    onClick: async () =>
                    {
                        captured.OnSelect();
                        _paletteOpen.Value = false;
                    },
                    content: btn =>
                    {
                        btn.Icon(["w-3.5 h-3.5 text-zinc-400"], name: item.Icon);
                        btn.Text(["text-sm text-zinc-200 truncate"], item.Label);
                    });
            }
        });
}
```

## Notes

- `BuildPaletteItems()` runs every render — items reflect current state (live cameras, current mute toggle label) without a separate refresh path.
- Wire opening to a `KeyboardListener` on the root that watches for `/`, `Cmd+K`, etc., and toggles `_paletteOpen.Value`.
- The `OnSelect` action is a plain `System.Action`, not `Func<Task>` — it's called synchronously from the button click and is expected to mutate reactives only. Long-running side effects belong in a separate task started by the action.
- Filter scoring is simple substring match here. For larger palettes, score title prefix matches higher.
- Always `_paletteOpen.Value = false` after picking to avoid leaving the dialog open behind the destination view.

## See also

- `collapsible-sidebar-nav` — the visual counterpart; both navigate the same `_activeSection` reactive
