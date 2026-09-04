<!-- mined-from: Ikon.App.Patterns -->
# Upscale An Image — Ask The Model What It Can Do

`ImageUpscaler` is capability-gated harder than the other AI services, and deliberately: a faithful
model **throws** rather than quietly ignoring a `Creativity` or `ScaleFactor` it does not support.
That is what stops a faithful upscale silently becoming a hallucinated one — so the model's own
flags decide what the config may carry.

## When to use

Recovering detail in a photo, preparing a generated image for print or a large display, cleaning up
a user upload. Not for resizing: a `w-` class does that for free and costs nothing.

## Notes

- **`using Ikon.AI.ImageUpscaling;` is required.** Like `Ikon.AI.Emergence.Tree`, a nested
  namespace is not imported by its parent, so `GlobalUsings.cs` does not cover it.
- Ask before you configure: `SupportsScaleFactor`, `MaxScaleFactor`, `SupportsCreativity`,
  `SupportsTargetResolution`, `SupportsFaceEnhancement`, `MaxOutputMegapixels`.
- **`ScaleFactor` and `TargetResolution` are mutually exclusive.** A factor above the model's
  `MaxScaleFactor` throws rather than clamping; `0` leaves the model's own default in place.
- Every default model is `UpscaleFidelity.Faithful`. Reach for `ImageUpscalerModel.Crystal` plus
  `Creativity` when you want a model to invent detail, and know that you are asking for it.
- `EnhanceFaces` invents detail even on an otherwise faithful model, so it is off unless asked for.
- `OutputFormat` defaults to PNG: re-encoding a freshly recovered image as JPEG throws away the
  detail just paid for.
- `MaxOutputMegapixels` is enforced **only when the input is bytes** — a URL's size is not known up
  front, so a URL source can still be refused by the provider.
- The result is `result.Image`, an `OutputImage` — `ResultKind` says whether it carries `Data` or
  `Url`, the same delivery contract as generation.
- `InputImage` is the shape every image-taking service uses: exactly one of `Data`, `Url` or
  `AssetUri`.

## Snippet

```csharp
private readonly Reactive<OutputImage?> _upscaled = new(null);
private readonly Reactive<bool> _busy = new(false);

/// <summary>
/// Upscaling is capability-gated harder than most services: a faithful model THROWS rather
/// than quietly ignoring Creativity or ScaleFactor it does not support, so the model's own
/// flags decide what the config may carry.
/// </summary>
private async Task UpscaleAsync(byte[] source, string mimeType)
{
    if (_busy.Value)
    {
        return;
    }

    using var _ = _busy.AsToken();

    try
    {
        using var upscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);

        var result = await upscaler.UpscaleImageAsync(new ImageUpscalerConfig
        {
            // InputImage is the same shape every image-taking service uses: exactly one of
            // Data, Url or AssetUri.
            InputImage = new InputImage { Data = source, MimeType = mimeType },

            // ScaleFactor and TargetResolution are mutually exclusive. Asking for a factor
            // above MaxScaleFactor throws rather than clamping, so ask the model first.
            ScaleFactor = upscaler.SupportsScaleFactor ? Math.Min(2.0, upscaler.MaxScaleFactor) : 0,

            // Creativity lets a model invent detail, and only models reporting
            // SupportsCreativity accept a non-zero value -- which is what stops a faithful
            // upscale quietly turning into a hallucinated one.
            Creativity = upscaler.SupportsCreativity ? 0.2 : 0,
        });

        _upscaled.Value = result.Image;
    }
    catch (AIException)
    {
        // Upscaling failed; the original is untouched and the user can retry.
    }
}

private void Render(IView view)
{
    if (_upscaled.Value is not { } image)
    {
        return;
    }

    // ResultKind says which of Data/Url this result actually carries, decided by
    // ResultDelivery -- the same contract as generation.
    if (image.Kind == ResultKind.Data && image.Data is { } bytes)
    {
        view.Image(["rounded-lg"], data: bytes, mimeType: image.MimeType, alt: "Upscaled");
    }
    else if (image.Url is { } url)
    {
        view.Image(["rounded-lg"], src: url, alt: "Upscaled");
    }
}
```

## See also

- `generated-image-with-result` — reading a generation result, same delivery contract.
