<!-- mined-from: Sentinel -->
# Persistent User Preferences — Per-user reactives that survive restarts

`PersistentUserReactive<T>` is `Reactive<T>` that automatically persists per-user. Set the value once and it sticks — across reloads, across deploys, across new sessions for that same user. Use it for any setting the user expects to remember: collapsed sidebar, view mode, last selected tab, density preference. `PersistentSessionReactive<T>` is the same idea but scoped to a session (shared across users in a game), and `PersistentReactive<T>` is global to the app.

## When to use

Anywhere you'd previously declare `private readonly Reactive<bool> _sidebarCollapsed = new(false);` and the user would lose their setting on every reconnect. The wrong choice here is the most common bug — devs reach for `Reactive<T>` because it has a familiar API, and the user's preference dies on the next page load. Default to `PersistentUserReactive<T>` for any boolean / enum / string preference toggle.

## Snippet

```csharp
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
```

## Notes

- Decision rule: ask "if the same user reconnects tomorrow, do they expect this state preserved?" — yes → `PersistentUserReactive`, no → `ClientReactive`.
- Don't use `PersistentReactive<T>` (global) for user preferences — every user would share the same value. That one is for app-wide config like a quiz's correct answer.
- Don't use `PersistentSessionReactive<T>` for per-user prefs in a multi-user app either — operators would overwrite each other's preferences. It's correct for shared facts (the event log all operators see) where you want persistence but no per-user split.
- The reactive system stores the values via the platform's persistence backend; no extra wiring is needed in the app code. The `new(defaultValue)` argument is only used the first time the user has no stored value.
- Reading and writing both go through `.Value` exactly like `Reactive<T>` — the persistence is invisible at the call site.

## See also

- `collapsible-sidebar-nav` — the canonical consumer of `PersistentUserReactive<bool>`
- `typical-app-structure` — the broader picture of reactive scopes
