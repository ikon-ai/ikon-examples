<!-- mined-from: QTribunal -->
# Threshold-Driven Regenerate — Bucketed Side-Effects On A Continuous Signal

A continuous signal (game proximity 0-1, sentiment score, attention level) is bucketed into a small set of thresholds (0, 0.3, 0.6). Each tick, the new bucket is compared against `_lastImageProximity`'s bucket; only when the user crosses *upward* into a new bucket does the expensive side-effect (image regeneration) fire. Keeps a generative pipeline responsive to progress without thrashing on every micro-change.

## When to use

Any expensive transform (image regeneration, sound design pass, LLM rewrite) keyed off a smooth slider/score that you want to react in roughly 3 visual states, not continuously. Fixes the "regenerate-every-tick" trap that explodes cost. Avoid when the operation is cheap — just regenerate every change.

## Snippet

```csharp
private readonly Reactive<float> _proximity = new(0f);
private readonly Reactive<float> _lastImageProximity = new(-1f);
private readonly Reactive<byte[]?> _sceneImageData = new(null);
private readonly Reactive<string?> _sceneImageMime = new(null);

private async Task MaybeRegenerateImageAsync(CancellationToken ct)
{
    if (_currentAccusation == null)
    {
        return;
    }

    var currentThreshold = _proximity.Value switch
    {
        >= 0.6f => 0.6f,
        >= 0.3f => 0.3f,
        _ => 0f
    };

    var lastThreshold = _lastImageProximity.Value switch
    {
        >= 0.6f => 0.6f,
        >= 0.3f => 0.3f,
        _ => 0f
    };

    if (currentThreshold <= lastThreshold)
    {
        return;
    }

    var imageResult = await GenerateSceneImageAsync(_currentAccusation.Scene.ImagePrompt, _proximity.Value, ct);

    if (imageResult != null)
    {
        _sceneImageData.Value = imageResult.Value.Data;
        _sceneImageMime.Value = imageResult.Value.MimeType;
        _lastImageProximity.Value = _proximity.Value;
    }
}
```

## Notes

- Initialize `_lastImageProximity = -1f` so the first call is *always* below any real threshold and the first regeneration fires unconditionally — no special-case bootstrap branch.
- Compare *thresholds* not raw values. With raw values, oscillation around 0.6 (0.59 ↔ 0.61) would burn images on every flicker.
- Allow `currentThreshold <= lastThreshold` to skip — both equal-bucket *and* downward moves are no-ops. If you also want to react when the user drops back, change to `!=` and store both directions.
- Pass the raw signal (not the threshold) to the generator — the prompt can use the precise value to influence detail intensity, even though the *trigger* is bucketed.

## See also

- `score-bar-meter` — visualizing the same continuous signal that drives the buckets
- `weighted-progress-banner` — alternative when you want a smooth bar instead of bucketed regenerate
