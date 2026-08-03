// Generation stages, surfaced to the user as staged progress text.
public enum Stage
{
    Idle,
    GeneratingImage,
    EstimatingDepth,
    Ready,
    Error
}

// Depth-parallax techniques, in increasing order of quality and cost. Passed to the shader as the
// uAlgorithm uniform; switching is a live uniform update with no regeneration.
public enum ParallaxAlgorithm
{
    DirectOffset = 0,
    IterativeRefine = 1,
    SteepParallax = 2,
    OcclusionMapping = 3
}

public partial class DepthParallaxApp
{
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(_prompt.Value))
        {
            return;
        }

        _error.Value = null;
        _showDepth.Value = false;
        _stage.Value = Stage.GeneratingImage;

        try
        {
            using var imageGenerator = new ImageGenerator(_imageModel.Value);
            var images = await imageGenerator.GenerateImageAsync(new ImageGeneratorConfig
            {
                Prompt = _prompt.Value,
                Width = 1408,
                Height = 768
            });

            var image = images.FirstOrDefault();

            if (image is null)
            {
                Fail("Image generation returned no image");
                return;
            }

            var imageData = await image.GetDataAsync();

            if (imageData.Length == 0)
            {
                Fail("Image generation returned no image");
                return;
            }

            _imageDataUri.Value = ToDataUri(image.MimeType, imageData);

            _stage.Value = Stage.EstimatingDepth;

            using var depthEstimator = new DepthEstimator(_depthModel.Value);
            var depth = await depthEstimator.EstimateDepthAsync(new DepthEstimatorConfig
            {
                InputImage = new InputImage
                {
                    Data = image.Data,
                    MimeType = image.MimeType
                }
            });

            var depthData = await depth.Depth.GetDataAsync();

            if (depthData.Length == 0)
            {
                Fail("Depth estimation returned no image");
                return;
            }

            _depthDataUri.Value = ToDataUri(depth.Depth.MimeType, depthData);
            _stage.Value = Stage.Ready;
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Depth parallax generation failed: {ex}");
            Fail(ex.Message);
        }
    }

    private void Fail(string message)
    {
        _error.Value = message;
        _stage.Value = Stage.Error;
    }

    private static string ToDataUri(string mimeType, byte[] data)
    {
        var mime = string.IsNullOrWhiteSpace(mimeType) ? "image/png" : mimeType;
        return $"data:{mime};base64,{Convert.ToBase64String(data)}";
    }
}
