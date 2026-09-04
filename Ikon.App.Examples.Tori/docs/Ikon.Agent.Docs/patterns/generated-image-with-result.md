<!-- mined-from: Ikon.App.Patterns -->
# Generated Image With Result — Reading What ImageGenerator Hands Back

`ImageGenerator.GenerateImageAsync` returns a `List<ImageGeneratorResult>`, one entry per
`ImageGeneratorConfig.Count` — not a single image and not a URL string. Each result carries
**either** `Data` (bytes, with `MimeType`) **or** `Url`, decided by `ResultDelivery`, plus the
`Width`/`Height` actually produced. Rendering both branches is what keeps a view working when the
delivery mode changes.

## When to use

Any app that generates an image and then has to display, measure, download or store it — the
moment you need the result rather than just the call. For a one-off with no config, the static
`ImageGenerator.GenerateAsync(prompt)` returns a single `ImageGeneratorResult` directly.

Generation **throws** (`AIException`, or `NonRetryableAIException` for bad input) instead of
returning an empty result, so the app
decides what a missing image means; catching it is how the rest of the screen survives.

## Snippet

```csharp
private readonly Reactive<ImageGeneratorResult?> _image = new(null);
private readonly Reactive<bool> _busy = new(false);
private readonly Reactive<string?> _error = new(null);

private async Task GenerateAsync(string prompt)
{
    if (_busy.Value)
    {
        return;
    }

    _error.Value = null;
    using var _ = _busy.AsToken();

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
            Quality = ImageQuality.High,
        });

        _image.Value = results.FirstOrDefault();
    }
    catch (AIException ex)
    {
        // Generation throws rather than returning an empty result, so the app decides what a
        // missing image means; here it degrades to a message and keeps the previous one.
        _error.Value = ex.Message;
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

        // A result carries EITHER Data or Url, decided by ImageGeneratorConfig.ResultDelivery.
        // Rendering both cases is what makes the view survive a delivery-mode change.
        if (_image.Value is { } image)
        {
            if (image.Data is { } bytes)
            {
                col.Image(["rounded-lg"], data: bytes, mimeType: image.MimeType, alt: "Generated image");
            }
            else if (image.Url is { } url)
            {
                col.Image(["rounded-lg"], src: url, alt: "Generated image");
            }

            col.Text(["text-muted-foreground text-xs"], text: $"{image.Width}×{image.Height} {image.MimeType}");
        }
    });
}
```
