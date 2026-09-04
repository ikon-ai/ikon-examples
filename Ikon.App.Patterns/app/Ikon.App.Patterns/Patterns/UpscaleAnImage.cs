// Ikon.AI.ImageUpscaling is NOT in an app's GlobalUsings — like Ikon.AI.Emergence.Tree, a nested
// namespace is not imported by its parent, so an app that upscales adds this line itself.
using Ikon.AI.ImageUpscaling;

namespace Ikon.App.Patterns.Patterns;

// Pattern: upscale-an-image — see docs/patterns/upscale-an-image.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class UpscaleAnImage : IPatternDemo
{
    public string Slug => "upscale-an-image";
    public string Title => "Upscale an image";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-upscale-an-image
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
    #endregion
}
