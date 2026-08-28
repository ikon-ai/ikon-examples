<!-- mined-from: Ikon.App.Monitor -->
# Cloud-JSON Asset Store — Lightweight Per-Space Persistence Without a Database

For app-level documents (dashboard configs, settings, indices) that are bigger than a reactive but smaller than a SQL table, use `Asset.Instance.GetTextAsync` / `SetTextAsync` against an `AssetUri(AssetClass.CloudJson, ...)`. Per-space scoping is built in — no Postgres setup, no schema migration, no concurrency primitives.

## When to use

You need to load/save a JSON blob keyed by a stable name in the current space. Typical: a list of dashboards, an index of saved queries, a per-user chat history, a settings document. The blob is small (KB-MB) and read on demand — not hot.

## Snippet

```csharp
private AssetUri DashboardIndexUri =>
    new(AssetClass.CloudJson, "monitor/dashboards.json", spaceId: app.GlobalState.SpaceId);

private AssetUri DashboardDataUri(string id) =>
    new(AssetClass.CloudJson, $"monitor/dashboard-{id}.json", spaceId: app.GlobalState.SpaceId);

private AssetUri ChatHistoryUri =>
    new(AssetClass.CloudFile, "chat-history.json",
        spaceId: app.GlobalState.SpaceId,
        userId: app.GlobalState.PrimaryUserId);

private async Task LoadDashboardsAsync()
{
    _isLoading.Value = true;
    try
    {
        var json = await Asset.Instance.GetTextAsync(DashboardIndexUri);

        if (!string.IsNullOrEmpty(json))
        {
            var index = JsonSerializer.Deserialize<DashboardIndex>(json);

            if (index != null)
            {
                _dashboards.ReplaceAll(index.Dashboards); // ReactiveList — one notification
            }
        }
    }
    catch
    {
        // First read is expected-empty (or corrupt) — seed defaults rather than throwing
        _dashboards.Clear();
        await SaveDashboardsAsync();
    }
    finally
    {
        _isLoading.Value = false;
    }
}

private async Task SaveDashboardsAsync()
{
    var index = new DashboardIndex { Dashboards = _dashboards.ToList() };
    var json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
    await Asset.Instance.SetTextAsync(DashboardIndexUri, json);
}
```

## Notes

- `AssetClass.CloudJson` is the right pick for structured documents; `CloudFile` for arbitrary blobs (chat history, etc.).
- Always treat the first read as expected-empty — wrap in `try/catch` and seed defaults rather than throwing.
- For per-user state (preferences, chat history) include `userId: app.GlobalState.PrimaryUserId` in the URI; for space-shared state omit it.
- Save on `app.StoppingAsync` for state that mutates often (chat history) so you don't pay write cost per turn.
- Pretty-print (`WriteIndented = true`) — these blobs are operator-readable in the asset browser.
- The in-memory mirror is a `ReactiveList<Dashboard>`: `ReplaceAll(loaded)` after a load, `Clear()` on a miss, `ToList()` when serialising back out. Each mutator notifies once; `_dashboards.Value` is an `IReadOnlyList<T>` snapshot, so there is no in-place mutation to forget.
- A binary in a connected repository is tracked as an `AssetPointer` — the small versioned `*.ikonasset` text file git stores in place of the bytes — so checking out any commit restores that commit's assets and binary history, undo and redo keep working. `BinaryContent` is the in-memory counterpart when you hold the bytes directly.

## See also

- `persistent-user-preferences`
- `typical-app-structure`
