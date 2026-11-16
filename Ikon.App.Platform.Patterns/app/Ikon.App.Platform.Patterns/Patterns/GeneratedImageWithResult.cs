namespace Ikon.App.Platform.Patterns.Patterns;

// Pattern: generated-image-with-result — see docs/patterns/generated-image-with-result.md.
// The example region below is the canonical body the doc extracts.
internal sealed class GeneratedImageWithResult(IAppBase app) : IPatternDemo
{
    public string Slug => "generated-image-with-result";
    public string Title => "Generated image, result in hand";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    #region example:pattern-generated-image-with-result
    private readonly Reactive<ImageGeneratorResult?> _image = new(null);
    private readonly Reactive<string?> _imageUrl = new(null);
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _error = new(null);

    // One path per instance: a write replaces the file and gives it a new URL, so regenerating
    // leaves no old files behind and the browser never shows a cached copy of the last image.
    private readonly string _imagePath = $"generated/{Guid.NewGuid():N}";

    private async Task GenerateAsync(string prompt)
    {
        if (_busy.Value)
        {
            return;
        }

        _error.Value = null;
        using var busy = _busy.AsToken();

        try
        {
            using var generator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);

            // GenerateImageAsync returns a LIST -- one entry per Count. The single-image
            // convenience is the static ImageGenerator.GenerateAsync(prompt).
            var results = await generator.GenerateImageAsync(new ImageGeneratorConfig
            {
                Prompt = prompt,
                Width = 1024,
                Height = 1024,
            });

            if (results.FirstOrDefault() is not { } image)
            {
                return;
            }

            // A result carries EITHER Data or Url, decided by ImageGeneratorConfig.ResultDelivery.
            // Both are written to the app's public files: inline bytes (about 2 MB for a Gemini
            // image) would reach every client inside the UI update, and a result URL expires
            // after about an hour. The browser loads the stored file by its URL.
            if (image.Data is { } bytes)
            {
                await app.Files.Public.WriteBytesAsync(_imagePath, bytes, image.MimeType);
            }
            else if (image.Url is { } url)
            {
                await app.Files.Public.WriteFromUrlAsync(_imagePath, url, image.MimeType);
            }
            else
            {
                return;
            }

            _imageUrl.Value = await app.Files.Public.GetUrlAsync(_imagePath);
            _image.Value = image;
        }
        catch (Exception ex) when (ex is AIException or HttpRequestException or TimeoutException)
        {
            // Generation throws rather than returning an empty result, so the app decides what a
            // missing image means; here it keeps the previous image and offers a retry. A result
            // URL that fails to download throws HttpRequestException, or TimeoutException after
            // 10 minutes. The exception text is for the log, never the screen.
            Log.Instance.Warning($"Image generation for '{prompt}' failed, user can retry: {ex.Message}");
            _error.Value = "Couldn't create the image. Try again.";
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-3"], content: col =>
        {
            col.Button(
                disabled: _busy.Value,
                onClick: async () => await GenerateAsync("a lighthouse in fog"),
                content: v => v.Text(text: _busy.Value ? "Generating…" : "Generate"));

            if (_error.Value is string error)
            {
                col.Text(["text-destructive text-sm"], text: error);
            }

            if (_image.Value is { } image && _imageUrl.Value is { } url)
            {
                col.Image(["rounded-lg"], src: url, alt: "Generated image");
                col.Text(["text-muted-foreground text-xs"], text: $"{image.Width}×{image.Height} {image.MimeType}");
            }
        });
    }
    #endregion
}
