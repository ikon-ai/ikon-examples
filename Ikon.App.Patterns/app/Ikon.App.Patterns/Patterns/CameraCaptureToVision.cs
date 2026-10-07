namespace Ikon.App.Patterns.Patterns;

// Pattern: camera-capture-to-vision — see docs/patterns/camera-capture-to-vision.md.
// The example region below is the canonical body the doc extracts.
internal sealed class CameraCaptureToVision : IPatternDemo
{
    public string Slug => "camera-capture-to-vision";
    public string Title => "Camera capture into a vision model";
    public string Category => "Device & sensors";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Reading(string Text);

    #region example:pattern-camera-capture-to-vision
    private readonly ClientReactive<string?> _result = new(null);
    private readonly ClientReactive<string?> _error = new(null);

    /// <summary>
    /// The capture comes back as an ENCODED file (a complete JPEG or PNG), not raw pixels, so it
    /// goes straight into a vision model, an asset or a file with no conversion step.
    /// </summary>
    private async Task ReadLabelAsync()
    {
        _error.Value = null;
        ClientImageCapture shot;

        try
        {
            shot = await ClientFunctions.CaptureImageAsync(new ClientImageCaptureOptions
            {
                Width = 1280,
                // Quality is meaningful only for JPEG; PNG is lossless and ignores it.
                Format = ClientImageCaptureFormat.Jpeg,
                Quality = 0.8,
            });
        }
        catch (NotSupportedException)
        {
            // A client that cannot capture at all throws rather than returning empty -- say so
            // plainly instead of leaving a button that appears to do nothing.
            _error.Value = "This device can't capture photos.";
            return;
        }
        catch (FunctionCallException)
        {
            // A capture that fails on the client (no camera, permission denied, cancelled)
            // comes back as the remote call's failure.
            _error.Value = "Couldn't use the camera.";
            return;
        }

        // Width/Height are what the client ACTUALLY produced, which can differ from what was
        // asked for. Read them rather than assuming the request was honoured.
        Log.Instance.Debug($"Captured {shot.Width}x{shot.Height} {shot.Mime}");

        // An image reaches a model as an ImagePart inside a user MessageBlock -- there is no
        // AddImage helper on KernelContext.
        List<IMessagePart> parts =
        [
            new TextPart("Read the label in this photo."),
            new ImagePart(shot.Data, shot.Mime),
        ];

        try
        {
            var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, parts));
            var reading = await Emerge.Run<Reading>(LLMModel.Claude46Sonnet, context, pass => { });

            _result.Value = reading.Text;
        }
        catch (Exception ex)
        {
            // A stopped run throws EmergenceStoppedException, not an AIException, and a remote
            // failure that is not an AI error (a policy denial) is a FunctionCallException.
            Log.Instance.Warning($"Label reading for client {ReactiveScope.ClientId} failed, user can scan again: {ex.Message}");
            _error.Value = "Couldn't read that photo — try again.";
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            col.Button(onClick: ReadLabelAsync, content: v => v.Text(text: "Scan label"));

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], text: error);
            }

            if (_result.Value is { } result)
            {
                col.Text(text: result);
            }
        });
    }
    #endregion
}
