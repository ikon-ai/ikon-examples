<!-- mined-from: Transcript -->
# Saved Runs Sidebar — Cloud-JSON Index Plus Per-Item Asset

A right-hand sidebar listing past runs (transcripts, generations, exports). The list itself is a `List<TEntry>` persisted as one `CloudJson` index file scoped per user; each entry references heavier per-item asset URIs (the actual transcript text, audio file). Clicking "Load" fetches the per-item asset and pushes its contents into the live editing reactive. The active item gets a colored border.

## When to use

Apps that produce one-shot results the user wants to revisit — transcripts, document analyses, image-generation sessions, render exports. Keeps the index cheap to load (one round-trip on app start) while letting the heavy payload live in its own asset and load lazily on demand.

## Snippet

```csharp
public sealed record TranscriptEntry(
    string Id, string FileName, string AudioAssetUri, string TranscriptAssetUri,
    string Language, double DurationSeconds, string Summary,
    IReadOnlyList<string> ActionItems, DateTimeOffset CreatedAt);

private readonly ReactiveList<TranscriptEntry> _transcripts = new();
private readonly Reactive<string?> _activeTranscriptId = new(null);

private AssetUri BuildTranscriptIndexUri(string userId) => new(
    AssetClass.CloudJson, "transcripts/index.json",
    spaceId: app.GlobalState.SpaceId, userId: userId);

private async Task LoadTranscriptHistoryAsync()
{
    var userId = ResolveUserId();
    if (string.IsNullOrWhiteSpace(userId)) return;

    var entries = await Asset.Instance.TryGetAsync<List<TranscriptEntry>>(BuildTranscriptIndexUri(userId));
    if (entries == null) return;

    _transcripts.ReplaceAll(entries);
}

private async Task SaveTranscriptEntryAsync(TranscriptEntry entry)
{
    _transcripts.Insert(0, entry);
    var updated = new List<TranscriptEntry>(_transcripts.Peek);

    var userId = ResolveUserId();
    if (string.IsNullOrWhiteSpace(userId)) return;
    await Asset.Instance.SetAsync(BuildTranscriptIndexUri(userId), updated, new AssetMetadata(mimeType: MimeTypes.ApplicationJson));
}

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
```

## Notes

- Insert new entries at index 0 (`_transcripts.Insert(0, entry)`) so newest appears at top — saves a separate sort step on every render.
- The index stores only metadata + asset URIs. Don't denormalize the full transcript text into the index — re-saving the entire history on every new entry gets quadratic.
- `_transcripts` is a `ReactiveList<TranscriptEntry>` — `Insert(0, entry)` / `ReplaceAll(entries)` each notify once and serialize internally, so no hand-rolled lock; two concurrent saves can't copy from a stale list. `Peek` gives an untracked snapshot for the asset write.
- `userId` resolution falls back to `ReactiveScope.TryGet<UserScope>` then `app.GlobalState.PrimaryUserId` then `"dev-user"` — works in dev with auth disabled and in prod with real users.

## See also

- `persistent-user-preferences` — for small key-value user state instead of an index of entries
- `expandable-detail-card` — alternative for inline preview rather than load-into-editor
