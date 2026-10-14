#region example:pattern-barcode-scan-usings
using StbImageSharp;
using ZXing;
#endregion

namespace Ikon.App.Patterns.Patterns;

// Pattern: barcode-scan — see docs/patterns/barcode-scan.md.
// The demo draws the scan card in its default state; taking the photo needs a client with a camera.
internal sealed class BarcodeScan : IPatternDemo
{
    public string Slug => "barcode-scan";
    public string Title => "Barcode and QR scan";
    public string Category => "Device & sensors";
    public void RenderDemo(IView view) => Render((UIView)view);

    #region example:pattern-barcode-scan
    private readonly ClientReactive<string?> _scanned = new(null);
    private readonly ClientReactive<string?> _scanError = new(null);
    private readonly ClientReactive<bool> _scanning = new(false);

    private async Task ScanAsync()
    {
        _scanError.Value = null;
        ClientImageCapture shot;

        try
        {
            shot = await ClientFunctions.CaptureImageAsync(new ClientImageCaptureOptions
            {
                Width = 1600,
                Format = ClientImageCaptureFormat.Jpeg,
                Quality = 0.9,
            });
        }
        catch (NotSupportedException)
        {
            _scanError.Value = "This device can't take photos.";
            return;
        }
        catch (FunctionCallException)
        {
            // No camera, permission denied or the user cancelled.
            _scanError.Value = "Couldn't use the camera.";
            return;
        }

        _scanning.Value = true;

        try
        {
            // Decoding is CPU work on a full photo, so it runs off the session's thread.
            var result = await Task.Run(() => DecodeBarcode(shot.Data));

            if (result is null)
            {
                _scanError.Value = "No code found. Hold it closer, flat and well lit, and scan again.";
                return;
            }

            _scanned.Value = $"{result.BarcodeFormat}: {result.Text}";
        }
        finally
        {
            _scanning.Value = false;
        }
    }

    // ZXing reads pixels, not a JPEG: StbImageSharp decodes the capture to RGBA first.
    private static Result? DecodeBarcode(byte[] encoded)
    {
        var image = ImageResult.FromMemory(encoded, ColorComponents.RedGreenBlueAlpha);
        var source = new RGBLuminanceSource(image.Data, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options =
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.QR_CODE, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.UPC_A, BarcodeFormat.CODE_128],
            },
        };

        return reader.Decode(source);
    }

    private void Render(UIView view)
    {
        view.Column([Card.Default, "p-4 gap-3 max-w-md"], content: view =>
        {
            view.Text([Text.Body], _scanned.Value ?? "Point the camera at a barcode or QR code");

            if (_scanError.Value is { } error)
            {
                view.Text([Text.Caption, "text-error-primary"], error);
            }

            view.Button([Button.PrimaryMd, "self-start"], text: _scanning.Value ? "Reading…" : "Scan", disabled: _scanning.Value,
                onClick: ScanAsync);
        });
    }
    #endregion
}
