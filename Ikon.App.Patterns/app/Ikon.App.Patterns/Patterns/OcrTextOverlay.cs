using System.Globalization;

namespace Ikon.App.Patterns.Patterns;

// Pattern: ocr-text-overlay — see docs/patterns/ocr-text-overlay.md.
// The doc quotes the example region below. The demo shows a note instead, because the FileUpload
// needs a running app's upload handler.
internal sealed class OcrTextOverlay : IPatternDemo
{
    public string Slug => "ocr-text-overlay";
    public string Title => "Text found in a photo, overlaid where it was";
    public string Category => "Image & video";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "A photo is read with OCR, its paragraphs are translated in one Emerge call, and each translation is drawn in a box over the place its text was, positioned in percent of the photo. See docs/patterns/ocr-text-overlay.md.");

    #region example:pattern-ocr-text-overlay
    // Where one paragraph sits on the photo, in percent of the photo's width and height, so the box
    // stays in the same place at any size the photo is drawn.
    private sealed record TextBox(string Text, double Left, double Top, double Width, double Height);

    public sealed record Translation(List<string> Lines);

    private readonly ClientReactive<byte[]?> _photo = new(null);
    private readonly ClientReactiveList<TextBox> _boxes = new();
    private readonly ClientReactive<bool> _reading = new(false);
    private readonly ClientReactive<string?> _error = new(null);

    private async Task ReadAsync(int clientId, byte[] upload)
    {
        _reading.SetFor(clientId, true);
        _error.SetFor(clientId, null);
        _boxes.ReplaceAllFor(clientId, []);

        try
        {
            // OCR reads and the page shows these same bytes, so the boxes match what is on screen
            var (jpeg, _, _) = ImageUtils.EncodeJpeg(upload, maxDimension: 2000);
            _photo.SetFor(clientId, jpeg);

            var boxes = ToBoxes(await OCR.AnalyzeAsync(jpeg));

            if (boxes.Count == 0)
            {
                _error.SetFor(clientId, "No text found in that photo.");
                return;
            }

            // One call translates every paragraph. The lines are numbered so each translation goes back in its own box
            var translation = await Emerge.Run<Translation>(LLMModel.Default, pass =>
            {
                pass.Command = "Translate each numbered line into simple English a child can read. Return one line per input line, in the same order.\n"
                    + string.Join("\n", boxes.Select((box, index) => $"{index + 1}. {box.Text}"));
            });

            _boxes.ReplaceAllFor(clientId, boxes.Select((box, index) => box with { Text = translation.Lines.ElementAtOrDefault(index) ?? box.Text }));
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Reading the photo for client {clientId} failed, the user can try another: {ex.Message}");
            _error.SetFor(clientId, "Couldn't read that photo. Try another one.");
        }
        finally
        {
            _reading.SetFor(clientId, false);
        }
    }

    // A paragraph's polygon is a list of x,y pairs in the page's unit, which is pixels for a photo.
    // Dividing them by the page's size gives percentages.
    private static List<TextBox> ToBoxes(OCRResult result)
    {
        if (result.Pages.FirstOrDefault() is not { Width: > 0, Height: > 0 } page)
        {
            return [];
        }

        var boxes = new List<TextBox>();

        foreach (var paragraph in result.Paragraphs)
        {
            if (paragraph.BoundingRegions.FirstOrDefault(region => region.PageNumber == page.PageNumber) is not { Polygon.Count: >= 4 } region)
            {
                continue;
            }

            var xs = region.Polygon.Where((_, index) => index % 2 == 0).ToList();
            var ys = region.Polygon.Where((_, index) => index % 2 == 1).ToList();
            boxes.Add(new TextBox(paragraph.Content,
                Left: xs.Min() / page.Width * 100, Top: ys.Min() / page.Height * 100,
                Width: (xs.Max() - xs.Min()) / page.Width * 100, Height: (ys.Max() - ys.Min()) / page.Height * 100));
        }

        return boxes;
    }

    private static string Percent(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "%";

    private void Render(UIView view)
    {
        view.Column(["gap-3 max-w-xl"], content: view =>
        {
            view.FileUpload(accept: ["image/jpeg", "image/png"], capture: "environment", maxFileSize: 15 * 1024 * 1024,
                disabled: _reading.Value,
                onUploadComplete: async args =>
                {
                    if (args.LocalTempFilePath is not { } path)
                    {
                        return;
                    }

                    var upload = await File.ReadAllBytesAsync(path);
                    File.Delete(path);

                    // OCR and the model take seconds, longer than an upload hook may run
                    var clientId = ReactiveScope.ClientId;
                    _ = Task.Run(() => ReadAsync(clientId, upload));
                },
                label: "Choose or take a photo of a comic page", hint: "JPEG or PNG, up to 15 MB");

            if (_reading.Value)
            {
                view.Text([Text.Caption], "Reading and translating…");
            }

            if (_error.Value is { } error)
            {
                view.Text(["text-destructive text-sm"], error);
            }

            if (_photo.Value is { } photo)
            {
                // The photo sets the container's size; each box is placed over it in percent.
                view.Box(["relative w-full"], content: view =>
                {
                    view.Image(["block w-full h-auto rounded-lg"], data: photo, mimeType: "image/jpeg", alt: "The photo with its text translated");

                    foreach (var box in _boxes)
                    {
                        view.Box([$"absolute left-[{Percent(box.Left)}] top-[{Percent(box.Top)}] w-[{Percent(box.Width)}] h-[{Percent(box.Height)}] flex items-center overflow-hidden rounded-sm bg-white/90 px-0.5"],
                            content: view => view.Text(["text-[10px] leading-tight text-black"], box.Text));
                    }
                });
            }
        });
    }
    #endregion
}
