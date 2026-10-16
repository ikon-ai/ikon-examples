namespace Ikon.App.Patterns.Patterns;

// Pattern: command-palette-jump — see docs/patterns/command-palette-jump.md.
// The stream record and section/toggle reactives below stand in for the app state the palette
// items jump to and reflect.
internal sealed class CommandPaletteJump : IPatternDemo
{
    public string Slug => "command-palette-jump";
    public string Title => "Command palette jump";
    public string Category => "Navigation";

    public void RenderDemo(IView view)
    {
        view.KeyboardListener(keys: ["k"], requireCtrlOrMeta: true, preventDefault: true,
            onKeyDown: async _ => _paletteOpen.Value = !_paletteOpen.Value);

        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Button(["self-start px-3 py-1.5 rounded-md bg-zinc-800 ring-1 ring-zinc-700 text-sm text-zinc-100 hover:bg-zinc-700"],
                "Open command palette (Ctrl+K)",
                onClick: async () => _paletteOpen.Value = true);

            var focused = _focusedStreamId.Value is { } id && _streams.TryGetValue(id, out var stream) ? stream.CameraLabel : "none";
            col.Text(["text-sm text-zinc-400"],
                $"Section: {_activeSection.Value} · Focused camera: {focused} · Audio: {(_audioMuted.Value ? "muted" : "on")} · Add camera dialog: {(_addCameraOpen.Value ? "open" : "closed")}");
        });

        RenderCommandPalette(view);
    }

    private sealed record CameraStream(string StreamId, string CameraLabel);

    private readonly ConcurrentDictionary<string, CameraStream> _streams = new(new Dictionary<string, CameraStream>
    {
        ["cam-lobby"] = new("cam-lobby", "Lobby entrance"),
        ["cam-dock"] = new("cam-dock", "Loading dock"),
        ["cam-lot-b"] = new("cam-lot-b", "Parking lot B"),
        ["cam-server"] = new("cam-server", "Server room"),
    });
    private readonly ClientReactive<string> _activeSection = new("cameras");
    private readonly ClientReactive<bool> _addCameraOpen = new(false);
    private readonly ClientReactive<bool> _audioMuted = new(false);
    private readonly ClientReactive<string?> _focusedStreamId = new(null);

    #region example:pattern-command-palette-jump
    private readonly ClientReactive<bool> _paletteOpen = new(initialValue: false);
    private readonly ClientReactive<string> _paletteQuery = new(initialValue: "");

    private record PaletteItem(string Group, string Id, string Label, System.Action OnSelect);

    private List<PaletteItem> BuildPaletteItems()
    {
        var items = new List<PaletteItem>
        {
            new("Section", "section-cameras", "Cameras", () => _activeSection.Value = "cameras"),
            new("Section", "section-events", "Events", () => _activeSection.Value = "events"),
            new("Action", "add-camera", "Add camera", () => _addCameraOpen.Value = true),
            new("Action", "toggle-audio", _audioMuted.Value ? "Unmute audio" : "Mute audio",
                () => _audioMuted.Value = !_audioMuted.Value),
        };

        foreach (var s in _streams.Values.OrderBy(s => s.CameraLabel))
        {
            var sid = s.StreamId;
            items.Add(new("Camera", $"camera-{sid}", s.CameraLabel, () =>
            {
                _activeSection.Value = "cameras";
                _focusedStreamId.Value = sid;
            }));
        }

        return items;
    }

    private void RenderCommandPalette(UIView view)
    {
        var items = BuildPaletteItems();
        var groups = items
            .GroupBy(it => it.Group)
            .Select(g => new SelectOptionGroup(g.Key, g.Select(it => new SelectOption(it.Id, it.Label)).ToList()))
            .ToList();

        view.CommandPalette(
            groups,
            open: _paletteOpen.Value,
            onOpenChange: async open =>
            {
                _paletteOpen.Value = open;
                _paletteQuery.Value = "";
            },
            searchValue: _paletteQuery.Value,
            onSearchChange: async query => _paletteQuery.Value = query,
            placeholder: "Jump to camera, section, or action…",
            onSelect: async id =>
            {
                items.FirstOrDefault(it => it.Id == id)?.OnSelect();
                _paletteOpen.Value = false;
                _paletteQuery.Value = "";
            });
    }
    #endregion
}
