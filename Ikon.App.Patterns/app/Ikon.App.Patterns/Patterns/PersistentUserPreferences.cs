namespace Ikon.App.Patterns.Patterns;

// Pattern: persistent-user-preferences — see docs/patterns/persistent-user-preferences.md.
// The records outside the region stand in for the app's own identity, parameters, and domain models
// that the example's fields reference.
internal sealed class PersistentUserPreferences : IPatternDemo
{
    public string Slug => "persistent-user-preferences";
    public string Title => "Persistent user preferences";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Persistence pattern with no standalone demo surface: per-user, per-session, and per-client reactives that survive reloads for sidebar, view-mode, and sub-tab state. See the source and docs/patterns/persistent-user-preferences.md.");

    public sealed record SessionIdentity(string Id);
    public sealed record ClientParameters(string Id = "");
    private sealed record ThreatEvent(string Id, string Narration);
    private sealed record SentinelPreferences(string ViewMode);

    #region docsnippet:pattern-persistent-user-preferences
    public partial class SentinelApp(IApp<SessionIdentity, ClientParameters> app)
    {
        // Per-user, persists across sessions — sidebar state, view mode, sub-tabs
        private readonly PersistentUserReactive<bool> _sidebarCollapsed = new(false);
        private readonly PersistentUserReactive<string> _camerasViewMode = new("grid");
        private readonly PersistentUserReactive<bool> _camerasGroupByRole = new(false);
        private readonly PersistentUserReactive<string> _settingsSubTab = new("general");

        // Per-session, persists for the multi-user session — shared facts
        private readonly PersistentSessionReactiveList<ThreatEvent> _events = new();
        private readonly PersistentSessionReactive<SentinelPreferences?> _persistedPrefs = new(null);

        // Per-client only, no persistence — open dialogs, current selection
        private readonly ClientReactive<bool> _addCameraOpen = new(initialValue: false);
        private readonly ClientReactive<string?> _expandedEventId = new(initialValue: (string?)null);

        private void RenderSidebar(UIView view)
        {
            var collapsed = _sidebarCollapsed.Value;
            view.Button(
                ["px-2 py-1.5 hover:bg-zinc-800/60"],
                onClick: async () => _sidebarCollapsed.Value = !collapsed,
                content: btn => btn.Icon(["w-3.5 h-3.5"], name: collapsed ? "chevrons-right" : "chevrons-left"));
        }

        private void RenderViewModeToggle(UIView view)
        {
            var mode = _camerasViewMode.Value;
            view.Row(["gap-1"], content: row =>
            {
                row.Button([mode == "grid" ? "bg-zinc-800" : ""],
                    onClick: async () => _camerasViewMode.Value = "grid",
                    content: btn => btn.Icon([], name: "grid"));
                row.Button([mode == "list" ? "bg-zinc-800" : ""],
                    onClick: async () => _camerasViewMode.Value = "list",
                    content: btn => btn.Icon([], name: "list"));
            });
        }
    }
    #endregion

    // Outside the docsnippet: the pattern declares reactives for every scope (per-user, per-session,
    // per-client), but the trimmed example only renders the sidebar and view-mode toggle. Reference the
    // rest here so the warnings-as-errors build does not flag the fields no panel above reads.
    partial class SentinelApp
    {
        private void KeepPatternFieldsReferenced()
        {
            _ = _camerasGroupByRole.Value;
            _ = _settingsSubTab.Value;
            _ = _events.Count;
            _ = _persistedPrefs.Value;
            _ = _addCameraOpen.Value;
            _ = _expandedEventId.Value;
        }
    }
}
