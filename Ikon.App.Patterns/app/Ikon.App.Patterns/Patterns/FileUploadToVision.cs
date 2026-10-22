namespace Ikon.App.Patterns.Patterns;

// Pattern: file-upload-to-vision — see docs/patterns/file-upload-to-vision.md.
internal sealed class FileUploadToVision : IPatternDemo
{
    public string Slug => "file-upload-to-vision";
    public string Title => "Photo upload into a vision model";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "A photo picked with FileUpload arrives as a temp file, is read once, made smaller and sent to a vision model as an ImagePart; the answer shows under the button. See docs/patterns/file-upload-to-vision.md.");

    #region example:pattern-file-upload-to-vision
    private sealed record PlantId(string CommonName, string LatinName, string CareLevel);

    private readonly ClientReactive<PlantId?> _plant = new(null);
    private readonly ClientReactive<bool> _identifying = new(false);
    private readonly ClientReactive<string?> _error = new(null);

    private void RenderPhotoUpload(UIView view)
    {
        view.Column(["gap-3 max-w-md"], content: col =>
        {
            // On a phone, capture: "environment" offers the back camera beside the photo library
            col.FileUpload(accept: ["image/jpeg", "image/png"], capture: "environment", maxFileSize: 15 * 1024 * 1024,
                disabled: _identifying.Value,
                // No start hook: a photo is small, so it arrives as a temp file that is read once
                onUploadComplete: async args =>
                {
                    if (args.LocalTempFilePath is not { } path)
                    {
                        return;
                    }

                    var photo = await File.ReadAllBytesAsync(path);
                    File.Delete(path);

                    // The model takes seconds, and this hook must return within 50 s of the last chunk
                    var clientSessionId = ReactiveScope.ClientId;
                    _ = Task.Run(() => IdentifyAsync(clientSessionId, photo));
                },
                label: "Choose or take a photo of the plant", hint: "JPEG or PNG, up to 15 MB");

            if (_identifying.Value)
            {
                col.Text([Text.Caption], "Looking at your photo…");
            }

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], error);
            }

            if (_plant.Value is { } plant)
            {
                col.Text([Text.H3], plant.CommonName);
                col.Text([Text.Caption], $"{plant.LatinName} · {plant.CareLevel} care");
            }
        });
    }

    private async Task IdentifyAsync(int clientSessionId, byte[] photo)
    {
        _identifying.SetFor(clientSessionId, true);
        _error.SetFor(clientSessionId, null);

        try
        {
            byte[] jpeg;

            try
            {
                // A phone photo is several megabytes; the model reads 1568 px at most anyway
                (jpeg, _, _) = ImageUtils.EncodeJpeg(photo, maxDimension: 1568);
            }
            catch (Exception ex)
            {
                // The bytes are the client's claim: a renamed file or a format the decoder lacks lands here
                Log.Instance.Debug($"Photo of {photo.Length} bytes from client {clientSessionId} could not be decoded: {ex.Message}");
                _error.SetFor(clientSessionId, "That file isn't a photo this app can read. Use a JPEG or PNG.");
                return;
            }

            var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User,
                [new TextPart("Which plant is this?"), new ImagePart(jpeg, "image/jpeg")]));

            var plant = await Emerge.Run<PlantId>(LLMModel.Claude55Sonnet, context, pass =>
            {
                pass.SystemPrompt = "You identify house plants from photos. CareLevel is Easy, Medium or Hard.";
            });

            _plant.SetFor(clientSessionId, plant);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Plant identification for client {clientSessionId} failed, the user can upload again: {ex.Message}");
            _error.SetFor(clientSessionId, "Couldn't identify that photo. Try again.");
        }
        finally
        {
            _identifying.SetFor(clientSessionId, false);
        }
    }
    #endregion
}
