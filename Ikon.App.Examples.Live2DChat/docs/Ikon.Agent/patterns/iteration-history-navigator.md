<!-- mined-from: Ikon.App.CoPlanAI -->
# Iteration History Navigator — Prev/Next Through Past Generations

Each AI generation increments a workshop iteration counter and gets persisted. Prev/Next buttons navigate backwards through history: when leaving the latest iteration, the current `_generationState` is snapshotted into a per-client dictionary; when navigating back to "latest", that snapshot is restored. Older iterations are loaded fresh from the DB. The user always sees a clear distinction between "live, editable" and "historical, read-only".

## When to use

Image generators, draft writers, plan iterators — any creative app where users want to scrub back through past attempts without losing the in-progress one. Especially valuable when the latest iteration has unsaved client-side state (zoom, focused image, brush strokes) that you don't want to clobber.

## Snippet

```csharp
private readonly ClientReactive<Guid?> _currentActivityId = new((Guid?)null);
private readonly ClientReactive<int> _currentWorkshopIteration = new(0);
private readonly ClientReactive<int?> _viewedIterationNumber = new((int?)null);
private readonly ClientReactive<bool> _iterationNavigationLoading = new(false);
private readonly ConcurrentDictionary<int, GenerationState?> _latestIterationGenerationStates = new();

private async Task NavigateWorkshopIterationAsync(int direction)
{
    var activityId = _currentActivityId.Value;
    if (activityId == null || _iterationNavigationLoading.Value) return;

    var currentIteration = _viewedIterationNumber.Value ?? _currentWorkshopIteration.Value;
    var targetIteration = currentIteration + direction;

    if (targetIteration < 1 || targetIteration > _currentWorkshopIteration.Value) return;

    _iterationNavigationLoading.Value = true;
    try
    {
        var clientId = ReactiveScope.ClientId;

        // Snapshot the live state on first step away from latest (strip image bytes to keep it cheap)
        if (_viewedIterationNumber.Value == null)
        {
            var current = _generationState.Value;
            _latestIterationGenerationStates[clientId] = current with
            {
                Images = current.Images.Select(img => img with { Data = null, DataUrl = null }).ToList(),
                Slots = current.Slots.Select(s => s.Image != null
                    ? s with { Image = s.Image with { Data = null, DataUrl = null } }
                    : s).ToList()
            };
        }

        // Stepping back to the live one: restore snapshot, clear viewed marker
        if (targetIteration == _currentWorkshopIteration.Value)
        {
            if (_latestIterationGenerationStates.TryRemove(clientId, out var savedState) && savedState != null)
                _generationState.Value = savedState;

            _viewedIterationNumber.Value = null;
            return;
        }

        await LoadWorkshopIterationImagesAsync(activityId.Value, targetIteration);
        _viewedIterationNumber.Value = targetIteration;
    }
    finally
    {
        _iterationNavigationLoading.Value = false;
    }
}
```

## Notes

- Snapshots live in a `ConcurrentDictionary<int, ...>` keyed by `ClientId` because each viewer can be on a different iteration.
- `_viewedIterationNumber == null` is the canonical "viewing latest" state — simpler than a separate boolean.
- Strip heavy fields (`Data`, `DataUrl` blobs) before snapshotting — they re-resolve from the remote URL on restore.
- `_iterationNavigationLoading` gates concurrent clicks so spamming Prev doesn't race with the DB load.

## See also

- `time-scrubber-replay`
- `undo-redo-cursor-history`
- `image-gallery`
