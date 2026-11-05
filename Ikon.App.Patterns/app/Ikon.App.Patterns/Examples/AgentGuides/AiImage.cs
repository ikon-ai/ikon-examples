namespace Ikon.App.Patterns.Examples;

internal sealed partial class AgentGuideExamples
{

    private static async Task<string> DocImageUpscaleOneShotAsync(byte[] imageBytes)
    {
        #region example:image-upscale-one-shot
        var result = await ImageUpscaler.UpscaleAsync(imageBytes, "image/png", scaleFactor: 4);  // SeedVr2 by default
        var bytes = await result.Image.GetDataAsync();
        #endregion

        return $"{bytes.Length} bytes";
    }

    private static async Task<string> DocImageUpscaleConfigAsync()
    {
        #region example:image-upscale-config
        using var imageUpscaler = new ImageUpscaler(ImageUpscalerModel.SeedVr2);
        var result = await imageUpscaler.UpscaleImageAsync(new ImageUpscalerConfig
        {
            InputImage = new InputImage { Url = "https://example.com/photo.png" },
            TargetResolution = UpscaleTargetResolution.Uhd2160
        });
        #endregion

        return result.Image.MimeType ?? "unknown";
    }

    private static async Task DocImageGenerateConfigAsync()
    {
        #region example:image-generate-config
        using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
        var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
        {
            Prompt = "A neon-lit cyberpunk street",
            Width = 512,
            Height = 512
        });
        if (results.Count > 0) { var image = results[0]; /* await image.GetDataAsync(), image.MimeType */ }
        #endregion
    }

    private static async Task DocImageEditConfigAsync(byte[] photoBytes)
    {
        #region example:image-edit-input
        using var imageGenerator = new ImageGenerator(ImageGeneratorModel.Gemini25FlashImage);
        var results = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
        {
            Prompt = "Edit the provided image. Keep its composition and people; change only the lighting to sunset.",
            InputImages = [new InputImage { Data = photoBytes, MimeType = "image/jpeg", Type = InputImageType.Normal }],
            Width = 0,  // 0 and 0: keep the input's shape instead of the 1024 x 1024 square
            Height = 0
        });
        #endregion

        _ = results;
    }

    private static async Task<string> GenerateImageOneShotAsync(string prompt)
    {
        #region example:image-generate-one-shot
        var image = await ImageGenerator.GenerateAsync("A neon-lit cyberpunk street");  // Gemini25FlashImage (cheap+fast) by default
        var bytes = await image.GetDataAsync();  // payload bytes, downloaded transparently when delivered as a URL
        // image.MimeType — never null; throws AIException on failure
        #endregion

        return $"{bytes.Length} bytes, {image.MimeType}";
    }
}
