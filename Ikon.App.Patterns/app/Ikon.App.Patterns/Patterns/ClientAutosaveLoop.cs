namespace Ikon.App.Patterns.Patterns;

// Pattern: client-autosave-loop — see docs/patterns/client-autosave-loop.md.
// The stubs outside the region stand in for the editor's real page state and dirty-checking.
internal sealed class ClientAutosaveLoop : IPatternDemo
{
    public string Slug => "client-autosave-loop";
    public string Title => "Client autosave loop";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: runs a per-client cancellable timer loop that autosaves dirty editor state and cleans up on disconnect. See the source and docs/patterns/client-autosave-loop.md.");

    private enum AppPage { Home, ManageWorkshop }

    private readonly ClientReactive<AppPage> _currentPage = new(AppPage.Home);
    private readonly ClientReactive<string?> _wsEditorId = new(null);
    private readonly ClientReactive<string> _wsName = new("");
    private readonly ClientReactive<string> _wsAutoSaveStatus = new("");

    private bool HasUnsavedChanges() => throw new NotImplementedException();

    private void ClearStaleAutoSaveStatus() => throw new NotImplementedException();

    #region docsnippet:pattern-client-autosave-loop
    private readonly ConcurrentDictionary<int, CancellationTokenSource?> _autoSaveCts = new();

    private void StartAutoSaveLoop(int clientId)
    {
        var cts = new CancellationTokenSource();
        _autoSaveCts[clientId] = cts;
        _ = RunAutoSaveLoopAsync(clientId, cts.Token);
    }

    private void StopAutoSaveLoop(int clientId)
    {
        if (_autoSaveCts.TryRemove(clientId, out var cts) && cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Racing disconnect paths may have already disposed this CTS
            }

            try
            {
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed by a concurrent leave — nothing to clean up
            }
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

                if (_currentPage.Value != AppPage.ManageWorkshop)
                {
                    continue;
                }

                if (_wsEditorId.Value == null)
                {
                    continue;
                }

                if (!HasUnsavedChanges())
                {
                    ClearStaleAutoSaveStatus();
                    continue;
                }

                await PerformAutoSaveAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the client disconnects and the loop's token is cancelled
        }
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
        await Task.CompletedTask;
    }
    #endregion
}
