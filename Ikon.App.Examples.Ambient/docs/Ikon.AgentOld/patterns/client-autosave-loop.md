<!-- mined-from: Ikon.App.CoPlanAI -->
# Client Autosave Loop — Per-Client Background Save Every 5 Seconds

Each client that opens an editor gets its own cancellation-token-driven loop: every 5 seconds, the loop enters that client's reactive scope, checks whether the current page is the editor and whether anything changed, then writes to the DB. The CTS lives in a `ConcurrentDictionary` keyed by `ClientId`, so disconnects cleanly stop the loop without touching anyone else's.

## When to use

Editors with non-trivial state (workshop builders, form wizards, prompt editors) where users expect zero-effort persistence. The per-client scope means each user only saves *their* draft, even though the app holds many drafts simultaneously.

## Snippet

```csharp
private readonly ConcurrentDictionary<int, CancellationTokenSource?> _autoSaveCts = new();

// On client join:
private void StartAutoSaveLoop(int clientId)
{
    var cts = new CancellationTokenSource();
    _autoSaveCts[clientId] = cts;
    _ = RunAutoSaveLoopAsync(clientId, cts.Token);
}

// On client leave:
private void StopAutoSaveLoop(int clientId)
{
    if (_autoSaveCts.TryRemove(clientId, out var cts) && cts != null)
    {
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        try { cts.Dispose(); } catch (ObjectDisposedException) { }
    }
}

private async Task RunAutoSaveLoopAsync(int clientId, CancellationToken cancellationToken)
{
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            using var _ = ReactiveScope.Use(new ClientScope(clientId));

            if (_currentPage.Value != AppPage.ManageWorkshop) continue;
            if (_wsEditorId.Value == null) continue;
            if (!HasUnsavedChanges()) { ClearStaleAutoSaveStatus(); continue; }

            await PerformAutoSaveAsync();
        }
    }
    catch (OperationCanceledException) { }
    catch (Exception ex)
    {
        Log.Instance.Warning($"Auto-save loop terminated unexpectedly: {ex.Message}");
    }
}

private async Task PerformAutoSaveAsync()
{
    if (string.IsNullOrWhiteSpace(_wsName.Value))
    {
        _wsAutoSaveStatus.Value = "Auto-save paused: Workshop name is required";
        return;
    }
    // ... validate, then persist via EF, then update _wsAutoSaveStatus.Value with last-saved time
}
```

## Notes

- Every iteration enters `ReactiveScope.Use(new ClientScope(clientId))` so `ClientReactive<T>` reads see the right client's draft.
- Surface a status string (`_wsAutoSaveStatus`) — "Saved at HH:MM", "Workshop name required", "Saving..." — so the user trusts the loop is working.
- Skip when not on the editor page; the loop keeps running but does nothing.
- Catch `ObjectDisposedException` on dispose — multiple disconnect paths can race.

## See also

- `persistent-user-preferences`
- `background-processing-pipeline`
- `busy-flag-loading`
