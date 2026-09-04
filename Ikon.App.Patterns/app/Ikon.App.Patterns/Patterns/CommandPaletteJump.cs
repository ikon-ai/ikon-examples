namespace Ikon.App.Patterns.Patterns;

// Pattern: command-palette-jump — see docs/patterns/command-palette-jump.md.
// The stream record and section/toggle reactives below stand in for the app state the palette
// items jump to and reflect.
internal sealed class CommandPaletteJump : IPatternDemo
{
    public string Slug => "command-palette-jump";
    public string Title => "Command palette jump";
    public string Category => "Navigation";
    public void RenderDemo(IView view) => RenderCommandPalette(view);

    private sealed record CameraStream(string StreamId, string CameraLabel);

    private readonly ConcurrentDictionary<string, CameraStream> _streams = new();
    private readonly ClientReactive<string> _activeSection = new("cameras");
    private readonly ClientReactive<bool> _addCameraOpen = new(false);
    private readonly ClientReactive<bool> _audioMuted = new(false);
    private readonly ClientReactive<string?> _focusedStreamId = new(null);

    #region docsnippet:pattern-command-palette-jump
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
    #endregion
}
