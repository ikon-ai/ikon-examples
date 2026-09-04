namespace Ikon.App.Patterns.Patterns;

// Pattern: threshold-driven-regenerate — see docs/patterns/threshold-driven-regenerate.md.
// The records, the current-accusation field, and the throwing generator stub stand in for the
// caller's real scene state and expensive image pipeline.
internal sealed class ThresholdDrivenRegenerate : IPatternDemo
{
    public string Slug => "threshold-driven-regenerate";
    public string Title => "Threshold-driven regenerate";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title, "Regenerates an expensive scene image only when proximity crosses a discrete threshold — see docs/patterns/threshold-driven-regenerate.md.");

    private sealed record SceneInfo(string ImagePrompt);
    private sealed record Accusation(SceneInfo Scene);
    private readonly Accusation? _currentAccusation = null;
    private Task<(byte[] Data, string MimeType)?> GenerateSceneImageAsync(
        string imagePrompt, float proximity, CancellationToken ct) => throw new NotImplementedException();

    #region docsnippet:pattern-threshold-driven-regenerate
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
    #endregion
}
