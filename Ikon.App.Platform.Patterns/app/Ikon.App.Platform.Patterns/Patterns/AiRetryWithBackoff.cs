namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: ai-retry-with-backoff — see docs/patterns/ai-retry-with-backoff.md.
// The example region below is the canonical body the doc extracts.
internal sealed class AiRetryWithBackoff : IPatternDemo
{
    public string Slug => "ai-retry-with-backoff";
    public string Title => "Retry an AI call with backoff";
    public string Category => "AI";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-ai-retry-with-backoff
    private const int MaxAttempts = 3;

    private readonly ClientReactive<ImageGeneratorResult?> _image = new(null);
    private readonly ClientReactive<bool> _busy = new(false);
    private readonly ClientReactive<string?> _status = new(null);
    private readonly ClientReactive<string?> _error = new(null);

    private async Task GenerateAsync(string prompt)
    {
        if (_busy.Value)
        {
            return;
        }

        _error.Value = null;
        using var busy = _busy.AsToken();

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                _status.Value = attempt == 1 ? "Generating…" : $"Busy, trying again ({attempt} of {MaxAttempts})…";
                _image.Value = await ImageGenerator.GenerateAsync(prompt);
                _status.Value = null;
                return;
            }
            catch (RetryableAIException ex) when (attempt < MaxAttempts)
            {
                // Only a retryable failure is tried again: a timeout, throttling or an outage. Wait at
                // least as long as the provider asked, and longer after each failure.
                var backoff = TimeSpan.FromSeconds(2 * Math.Pow(2, attempt - 1));
                var delay = ex.RetryAfter is { } asked && asked > backoff ? asked : backoff;
                Log.Instance.Warning($"Image generation for '{prompt}' failed ({ModelFailure.Classify(ex)}), attempt {attempt} of {MaxAttempts}, retrying in {delay.TotalSeconds:F0} s: {ex.Message}");
                await Task.Delay(delay);
            }
            catch (AIException ex)
            {
                // The last attempt, or a failure a retry would repeat (a refused request, a denial,
                // a job the provider already charged for)
                Log.Instance.Warning($"Image generation for '{prompt}' failed after {attempt} attempt(s), user can try later: {ex.Message}");
                _status.Value = null;
                _error.Value = "Couldn't create the image. Try again later.";
                return;
            }
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                [Button.PrimaryMd],
                disabled: _busy.Value,
                onClick: async () => await GenerateAsync("a lighthouse in fog"),
                content: v => v.Text(text: _busy.Value ? "Generating…" : "Generate"));

            if (_status.Value is string status)
            {
                col.Text(["text-muted-foreground text-sm"], text: status);
            }

            if (_error.Value is string error)
            {
                col.Alert(error, tone: SemanticTone.Error);
            }

            if (_image.Value is { Data: { } bytes } image)
            {
                col.Image(["rounded-lg"], data: bytes, mimeType: image.MimeType, alt: "Generated image");
            }
            else if (_image.Value is { Url: { } url })
            {
                col.Image(["rounded-lg"], src: url, alt: "Generated image");
            }
            else
            {
                col.Text(["text-muted-foreground text-sm"], text: "Press Generate to make an image.");
            }
        });
    }
    #endregion
}
