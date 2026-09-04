namespace Ikon.App.Patterns.Patterns;

// Pattern: cloud-json-asset-store — see docs/patterns/cloud-json-asset-store.md.
// `app` gives the region the space/user scoping the AssetUris are keyed by; the record, index
// document, and reactive mirror below stand in for the app's real dashboard model.
internal sealed class CloudJsonAssetStore(IAppBase app) : IPatternDemo
{
    public string Slug => "cloud-json-asset-store";
    public string Title => "Cloud JSON asset store";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: persists a JSON index to the space-scoped asset store with optimistic concurrency. See the source and docs/patterns/cloud-json-asset-store.md.");

    private sealed record Dashboard(string Id, string Name);

    private sealed class DashboardIndex
    {
        public List<Dashboard> Dashboards { get; set; } = new();
    }

    private readonly ReactiveList<Dashboard> _dashboards = new();
    private readonly Reactive<bool> _isLoading = new(false);

    #region docsnippet:pattern-cloud-json-asset-store
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
    #endregion
}
