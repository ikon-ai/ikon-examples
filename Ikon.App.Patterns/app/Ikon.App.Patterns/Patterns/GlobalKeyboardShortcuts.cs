namespace Ikon.App.Patterns.Patterns;

// Pattern: global-keyboard-shortcuts — see docs/patterns/global-keyboard-shortcuts.md.
// The stubs outside the region stand in for the app's event model, its layer-state reactives, and the
// render/navigation helpers so the single-dispatcher listener and Esc-stack the doc extracts compile.
internal sealed class GlobalKeyboardShortcuts : IPatternDemo
{
    public string Slug => "global-keyboard-shortcuts";
    public string Title => "Global keyboard shortcuts";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "UI pattern whose demo needs app chrome to render: a single global KeyboardListener dispatches every shortcut and an Esc-stack closes the topmost open layer first. See the source and docs/patterns/global-keyboard-shortcuts.md.");

    private enum Severity { Info, Warning, Alert }
    private enum EventStatus { Open, Acknowledged, Dismissed }
    private sealed record SecurityEvent(string Id, Severity Severity, EventStatus Status);
    private readonly ReactiveList<SecurityEvent> _events = new();
    private readonly Reactive<bool> _audioMuted = new(false);
    private readonly Reactive<string> _activeSection = new("overview");
    private readonly Reactive<string> _paletteQuery = new("");
    private readonly Reactive<bool> _paletteOpen = new(false);
    private readonly Reactive<bool> _cheatSheetOpen = new(false);
    private readonly Reactive<bool> _addCameraOpen = new(false);
    private readonly Reactive<bool> _triageQueueOpen = new(false);
    private readonly Reactive<string?> _renamingStreamId = new(null);
    private readonly Reactive<string?> _expandedEventId = new(null);
    private readonly Reactive<string?> _focusedStreamId = new(null);
    private void RenderTopStrip(IView view) => throw new NotImplementedException();
    private void RenderAlertBanner(IView view) => throw new NotImplementedException();
    private void UpdateEventStatus(string eventId, EventStatus status) => throw new NotImplementedException();
    private void NavigateEventList(int direction) => throw new NotImplementedException();

    #region docsnippet:pattern-global-keyboard-shortcuts
    private void Render(IView view)
    {
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
    }

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
    #endregion
}
