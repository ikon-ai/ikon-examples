namespace Ikon.App.Patterns.Patterns;

// Pattern: region-pinned-ai — see docs/patterns/region-pinned-ai.md.
// The example region below is the canonical body the doc extracts.
internal sealed class RegionPinnedAi : IPatternDemo
{
    public string Slug => "region-pinned-ai";
    public string Title => "Keeping AI calls inside a region";
    public string Category => "Platform mechanics";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Server-side pattern with no UI: pinning AI calls to a data-residency region, and "
        + "checking the model actually serves it. See the source and docs/patterns/region-pinned-ai.md.");

    #region example:pattern-region-pinned-ai
    // Preference order, not a single choice: the platform takes the first region the model
    // actually serves. Listing a narrow region first and a broader one after is how you say
    // "as close as possible, but do not fail".
    private static readonly ModelRegion[] EuOnly = [ModelRegion.EuNorth, ModelRegion.EuWest, ModelRegion.Eu];

    // GetSupportedRegions returns the exact registered regions, while a requested Eu matches
    // every EU sub-region, so the check has to name them all.
    private static readonly ModelRegion[] EuRegions = [ModelRegion.Eu, ModelRegion.EuNorth, ModelRegion.EuWest, ModelRegion.EuCentral, ModelRegion.EuSouth];

    /// <summary>
    /// Unless the organisation's region policy is EU-only, residency is per-CLIENT: the regions
    /// are a constructor argument, so an app with a residency obligation constructs its
    /// generators rather than using the static one-shots, which run wherever the platform defaults.
    /// </summary>
    private static async Task<ImageGeneratorResult?> GenerateInEuAsync(string prompt)
    {
        const ImageGeneratorModel Model = ImageGeneratorModel.Gemini25FlashImage;

        // Ask BEFORE constructing. A model that serves no listed region makes the constructor
        // throw; checking first surfaces that at startup rather than at request time.
        var supported = ImageGenerator.GetSupportedRegions(Model);

        if (!supported.Any(EuRegions.Contains))
        {
            Log.Instance.Error($"{Model} serves none of the required regions — refusing to generate");
            return null;
        }

        using var generator = new ImageGenerator(Model, EuOnly);
        return await generator.GenerateAsync(prompt);
    }
    #endregion
}
