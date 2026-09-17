namespace Ikon.App.Patterns.Patterns;

// Pattern: saved-runs-sidebar — see docs/patterns/saved-runs-sidebar.md.
// The docsnippet region keeps a cheap per-user CloudJson index and loads heavier per-item assets on
// demand; the stubs outside it stand in for the host app, its user resolution and its load-into-editor.
internal sealed class SavedRunsSidebar(IAppBase app) : IPatternDemo
{
    public string Slug => "saved-runs-sidebar";
    public string Title => "Saved runs sidebar";
    public string Category => "Persistence";
    public void RenderDemo(IView view) => Render(view);

    private string ResolveUserId() => throw new NotImplementedException();
    private Task LoadTranscriptAsync(TranscriptEntry entry) => throw new NotImplementedException();

    #region docsnippet:pattern-saved-runs-sidebar
    public sealed record TranscriptEntry(
        string Id, string FileName, string AudioAssetUri, string TranscriptAssetUri,
        string Language, double DurationSeconds, string Summary,
        IReadOnlyList<string> ActionItems, DateTimeOffset CreatedAt);

    // The index file is per user, so the list it loads into is too: an unscoped ReactiveList is one
    // list shared by every client, and the last user to load would overwrite everyone's sidebar.
    // Which entry is open is per client -- it follows the editor on this device.
    private readonly UserReactiveList<TranscriptEntry> _transcripts = new();
    private readonly ClientReactive<string?> _activeTranscriptId = new(null);

    private AssetUri BuildTranscriptIndexUri(string userId) => new(
        AssetClass.CloudJson, "transcripts/index.json",
        spaceId: app.GlobalState.SpaceId, userId: userId);

    private async Task LoadTranscriptHistoryAsync()
    {
        var userId = ResolveUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var entries = await Asset.Instance.TryGetAsync<List<TranscriptEntry>>(BuildTranscriptIndexUri(userId));

        if (entries == null)
        {
            return;
        }

        // The ...For accessors name the user, so this also works from a joined handler or a
        // background load where no user scope is active.
        _transcripts.UpdateFor(userId, _ => entries);
    }

    private async Task SaveTranscriptEntryAsync(TranscriptEntry entry)
    {
        var userId = ResolveUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        _transcripts.UpdateFor(userId, existing => existing.Prepend(entry));
        var updated = new List<TranscriptEntry>(_transcripts.ValueFor(userId));

        await Asset.Instance.SetAsync(BuildTranscriptIndexUri(userId), updated, new AssetMetadata(mimeType: MimeTypes.ApplicationJson));
    }

    private void Render(IView view)
    {
        view.Column(["w-full lg:w-[320px] shrink-0"], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-2"], "Saved transcripts");

                foreach (var entry in _transcripts)
                {
                    var isActive = entry.Id == _activeTranscriptId.Value;
                    var cardStyle = isActive
                        ? "border border-brand-primary/60 bg-brand-primary/10"
                        : "border border-transparent";
                    view.Box([Card.Default, "p-4", cardStyle], content: view =>
                    {
                        view.Text([Text.Body, "font-semibold"], entry.FileName);
                        view.Text([Text.Caption], entry.CreatedAt.ToLocalTime().ToString("g"));
                        view.Button([Button.OutlineSm, "mt-3"], text: "Load",
                            onClick: async () => await LoadTranscriptAsync(entry));
                    });
                }
            });
        });
    }
    #endregion
}
