<!-- mined-from: Ikon.App.Monitor -->
# Dashboard With Panels CRUD — Edit-Mode Add/Edit/Clone/Delete on a Card Grid

A dashboard is a list of panel records. An "Edit" toggle reveals per-card pencil/copy/trash buttons; an "Add Panel" opens a dialog with the same form used for editing. All mutations rebuild the panel list immutably with `with { Panels = ... }` and persist through one shared save call.

## When to use

You're building any user-curated grid: dashboards, board layouts, saved searches, embed configs. The same form is shown for create and edit; clone is a copy-with-new-id; delete uses a confirmation dialog.

## Snippet

```csharp
private async Task<PanelConfig> AddPanelCoreAsync(string dashboardId, PanelConfig panel)
{
    var data = _currentDashboardData.Value ?? new DashboardData();
    _currentDashboardData.Value = data with { Panels = [..data.Panels, panel] };
    await SaveDashboardDataAsync(dashboardId);
    await ExecutePanelQueryAsync(panel);
    return panel;
}

private async Task<bool> EditPanelCoreAsync(string id, string panelId, string title, List<MonitoringQuery> queries)
{
    var existing = _currentDashboardData.Value?.Panels.FirstOrDefault(p => p.Id == panelId);
    if (existing == null) { return false; }

    var updated = new PanelConfig { Id = panelId, Title = title, Queries = queries };
    var panels = _currentDashboardData.Value!.Panels.Select(p => p.Id == panelId ? updated : p).ToList();
    _currentDashboardData.Value = _currentDashboardData.Value with { Panels = panels };
    await SaveDashboardDataAsync(id);
    await ExecutePanelQueryAsync(updated);
    return true;
}

private async Task ClonePanelAsync(PanelConfig panel, string id)
{
    var cloned = panel with { Id = Guid.NewGuid().ToString(), Title = $"{panel.Title} (Copy)" };
    var panels = new List<PanelConfig>(_currentDashboardData.Value!.Panels);
    var idx = panels.FindIndex(p => p.Id == panel.Id);
    panels.Insert(idx + 1, cloned);
    _currentDashboardData.Value = _currentDashboardData.Value with { Panels = panels };
    await SaveDashboardDataAsync(id);
    await ExecutePanelQueryAsync(cloned);
}

// Header row in edit mode:
view.Row(["gap-1"], content: row =>
{
    row.Button([Button.GhostMd, Button.Icon],
        onClick: async () => OpenPanelDialog(panel),
        content: v => v.Icon([Icon.Default], name: "pencil"));
    row.Button([Button.GhostMd, Button.Icon],
        onClick: async () => await ClonePanelAsync(panel, dashboardId),
        content: v => v.Icon([Icon.Default], name: "copy"));
    row.Button([Button.GhostMd, Button.Icon],
        onClick: async () => _deletePanelId.Value = panel.Id,
        content: v => v.Icon([Icon.Default], name: "trash-2"));
});
```

## Notes

- One `Reactive<DashboardData?>` for the dashboard, one `Reactive<bool>` for `_dashboardEditMode`. The edit toggle is the only user-visible affordance for entering CRUD.
- Always rebuild collections immutably (`with { Panels = ... }`) — the reactive only diff-broadcasts when the reference changes.
- Reuse the same dialog component for create and edit. Track `_editPanelId.Value` — null means "create".
- Pencil/copy/trash icons in a small Row; only render the row when `_dashboardEditMode.Value` is true. Less chrome at rest.
- Pair the trash icon with a separate `AlertDialog` confirmation; never delete on first click.

## See also

- `kpi-card-grid`
- `kanban-multi-column`
- `destructive-confirm-dialog`
