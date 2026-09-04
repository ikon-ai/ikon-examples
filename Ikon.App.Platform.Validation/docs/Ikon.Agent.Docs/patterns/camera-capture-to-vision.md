<!-- mined-from: Ikon.App.Patterns -->
# Camera Capture Into A Vision Model — Bytes That Are Already A File

`ClientFunctions.CaptureImageAsync` returns a `ClientImageCapture` whose `Data` is a **complete
JPEG or PNG file**, not raw pixels. It goes straight into a vision model, an asset or a file with
no conversion step — the encoding work is already done on the client.

## When to use

Scanning a label, a receipt, a whiteboard, a document, a shelf; anything where the user points a
camera and the app reads what is there. For a still the user picks from their library, that is
`FileUpload`.

## Notes

- **`Width`/`Height` on the result are what the client ACTUALLY produced**, which can differ from
  what was requested — a device may not honour a size. Read them rather than assuming.
- `Quality` is meaningful only for `ClientImageCaptureFormat.Jpeg`; PNG is lossless and ignores it.
  `Format` defaults to JPEG when null.
- **A client with no camera throws `NotSupportedException`.** Catch it and say so — otherwise the
  button appears to do nothing, which reads as a broken app rather than an unsupported device.
- **An image reaches a model as an `ImagePart` in a user `MessageBlock`.** There is no `AddImage`
  helper on `KernelContext`:
  `new KernelContext().Add(new MessageBlock(MessageBlockRole.User, [new TextPart(...), new ImagePart(bytes, mime)]))`.
- `Emerge.Run<T>` throws `EmergenceStoppedException`, which does **not** derive from `AIException` —
  catching the latter will not catch it.
- `ClientFunctions.GetVisibilityAsync` returns `ClientVisibility` (`Visible`, `Hidden`, `Unknown`):
  worth checking before capturing on a background tab, where a browser may refuse or stall.

## Snippet

```csharp
private readonly ClientReactive<string?> _result = new(null);
private readonly ClientReactive<string?> _error = new(null);

/// <summary>
/// The capture comes back as an ENCODED file (a complete JPEG or PNG), not raw pixels, so it
/// goes straight into a vision model, an asset or a file with no conversion step.
/// </summary>
private async Task ReadLabelAsync()
{
    _error.Value = null;

    try
    {
        var shot = await ClientFunctions.CaptureImageAsync(new ClientImageCaptureOptions
        {
            Width = 1280,
            // Quality is meaningful only for JPEG; PNG is lossless and ignores it.
            Format = ClientImageCaptureFormat.Jpeg,
            Quality = 0.8,
        });

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

        var context = new KernelContext().Add(new MessageBlock(MessageBlockRole.User, parts));
        var reading = await Emerge.Run<Reading>(LLMModel.Claude46Sonnet, context, pass => { });

        _result.Value = reading.Text;
    }
    catch (NotSupportedException)
    {
        // A client without a camera throws rather than returning empty -- say so plainly
        // instead of leaving a button that appears to do nothing.
        _error.Value = "This device has no camera.";
    }
    catch (EmergenceStoppedException)
    {
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
```

## See also

- `llm-vision-cache` — caching vision responses by image hash so a re-render costs nothing.
- `file-upload-with-progress` — the other way an image arrives.
