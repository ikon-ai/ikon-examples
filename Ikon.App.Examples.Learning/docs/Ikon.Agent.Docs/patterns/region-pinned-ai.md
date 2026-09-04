<!-- mined-from: Ikon.App.Patterns -->
# Keeping AI Calls Inside A Region

Every AI client takes an optional `IReadOnlyList<ModelRegion>`, and it is a **preference order**,
not a single choice: the platform uses the first region the model actually serves. Listing a narrow
region first and a broader one after is how you say *"as close as possible, but do not fail"*.

Residency is therefore **per-client, not global**. An app with a residency obligation constructs
its generators rather than calling the static one-shots, which run wherever the platform defaults.

## When to use

A data-residency requirement — EU customer data, a public-sector contract, an internal policy about
where inference happens.

## Notes

- **Ask `GetSupportedRegions(model)` before constructing.** A model that serves none of the
  required regions is a compliance failure worth surfacing at startup, not a silent fall-back to
  `Global` at request time.
- The enum runs from narrow to broad: `EuNorth`, `EuWest`, `EuCentral`, `EuSouth`, then `Eu`, then
  `Global`. Listing only narrow regions makes the call *fail* rather than leave the area, which is
  sometimes exactly what is wanted.
- **The static one-shots take no regions.** `ImageGenerator.GenerateAsync(prompt)` and its
  equivalents construct and dispose per call with platform defaults, so an app under a residency
  obligation cannot use them.
- Model availability varies by region far more than by capability, and a region a model served last
  quarter is not a promise. Checking at startup is what turns a silent policy breach into a boot
  failure.
- This governs where **inference** happens. Where the app's own data lives is a separate question —
  see the asset system and database guides.

## Snippet

```csharp
// Preference order, not a single choice: the platform takes the first region the model
// actually serves. Listing a narrow region first and a broader one after is how you say
// "as close as possible, but do not fail".
private static readonly ModelRegion[] EuOnly = [ModelRegion.EuNorth, ModelRegion.EuWest, ModelRegion.Eu];

/// <summary>
/// Residency is per-CLIENT, not global: the regions are a constructor argument, so an app
/// with a residency obligation constructs its generators rather than using the static
/// one-shots, which run wherever the platform defaults.
/// </summary>
private static async Task<ImageGeneratorResult?> GenerateInEuAsync(string prompt)
{
    const ImageGeneratorModel Model = ImageGeneratorModel.Gemini25FlashImage;

    // Ask BEFORE constructing. A model that serves no listed region is a compliance failure
    // to surface at startup, not a silent fallback to Global at request time.
    var supported = ImageGenerator.GetSupportedRegions(Model);

    if (!EuOnly.Any(supported.Contains))
    {
        Log.Instance.Error($"{Model} serves none of the required regions — refusing to generate");
        return null;
    }

    using var generator = new ImageGenerator(Model, EuOnly);
    return await generator.GenerateAsync(prompt);
}
```

## See also

- `generated-image-with-result` — the ordinary, unpinned path.
