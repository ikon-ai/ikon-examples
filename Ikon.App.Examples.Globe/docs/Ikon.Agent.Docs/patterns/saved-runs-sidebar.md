<!-- mined-from: Transcript -->
# Saved Runs Sidebar — Cloud-JSON Index Plus Per-Item Asset
<!-- checked-against: e198ebfaa7a0ce58 -->
A right-hand sidebar listing past runs (transcripts, generations, exports). The list itself is a `List<TEntry>` persisted as one `CloudJson` index file scoped per user and loaded into a `UserReactiveList<TEntry>` — the same scope in memory as on disk, so two users never see or overwrite each other's sidebar. Each entry references heavier per-item asset URIs (the actual transcript text, audio file). Clicking "Load" fetches the per-item asset and pushes its contents into the live editing reactive. The active item gets a colored border.

## When to use

Apps that produce one-shot results the user wants to revisit — transcripts, document analyses, image-generation sessions, render exports. Keeps the index cheap to load (one round-trip on app start) while letting the heavy payload live in its own asset and load lazily on demand.

## Snippet

```csharp
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
```

## Notes

- Prepend new entries (`existing.Prepend(entry)`) so newest appears at top — saves a separate sort step on every render.
- The index stores only metadata + asset URIs. Don't denormalize the full transcript text into the index — re-saving the entire history on every new entry gets quadratic.
- `_transcripts` is a `UserReactiveList<TranscriptEntry>`: an unscoped `ReactiveList` is one list shared by every client, so the last user to load would replace everyone's sidebar. `UpdateFor(userId, …)` notifies once and runs under that user's lock, so no hand-rolled lock; two concurrent saves can't copy from a stale list. `ValueFor(userId)` gives the snapshot for the asset write. Both name the user, so they work from a joined handler or a background load where no `UserScope` is active — inside `UI.Root()` or an action callback, enumerating `_transcripts` resolves the current user's list, and `Peek` reads it without tracking when a render must not subscribe to its own save.
- `_activeTranscriptId` is a `ClientReactive` — which entry is open follows the editor on this device, not the user's other sessions.
- `userId` resolution falls back to `ReactiveScope.TryGet<UserScope>` then `app.GlobalState.PrimaryUserId` then `"dev-user"` — works in dev with auth disabled and in prod with real users.

## See also

- `persistent-user-preferences` — for small key-value user state instead of an index of entries
