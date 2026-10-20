using Ikon.Pipelines.Public.Processors.Pdf;
using StbImageWriteSharp;

namespace Ikon.App.Patterns.Patterns;

// Pattern: pdf-viewer — see docs/patterns/pdf-viewer.md.
// The demo draws the viewer, and its button opens a two-page PDF built here in place of an upload,
// since the headless render has no upload handler.
internal sealed class PdfViewer(IAppBase app) : IPatternDemo
{
    public string Slug => "pdf-viewer";
    public string Title => "PDF viewer that works on phones";
    public string Category => "Media";

    public void RenderDemo(IView view)
    {
        RenderViewer((UIView)view);
        view.Button([Button.OutlineMd, "self-start mt-3"], text: "Open a sample score", disabled: _converting.Value,
            onClick: async () => await ShowPdfAsync(SamplePdf()));
    }

    #region example:pattern-pdf-viewer
    // The pages of the open PDF, each a JPEG in private storage. A real app keeps this list in the
    // document's database row.
    private readonly Reactive<IReadOnlyList<AssetUri>> _pages = new([]);
    private readonly ClientReactive<int> _pageIndex = new(0);
    private readonly Reactive<bool> _converting = new(false);
    private readonly Reactive<string?> _error = new(null);

    // 1600 px on the long side keeps small print readable when a phone zooms in
    private const int PagePixels = 1600;
    private const int MaxPages = 60;

    private void RenderUpload(UIView view)
    {
        // No start hook, so the file arrives as a temp file, which suits a document read once
        view.FileUpload(accept: [".pdf", "application/pdf"], maxFileSize: 30 * 1024 * 1024, disabled: _converting.Value,
            onUploadComplete: async args =>
            {
                if (args.LocalTempFilePath is not { } path)
                {
                    return;
                }

                var bytes = await File.ReadAllBytesAsync(path);
                File.Delete(path);

                // Rendering takes seconds, and this hook must return within 50 s of the last chunk
                _ = Task.Run(() => ShowPdfAsync(bytes));
            },
            content: zone => zone.Text([Text.Body], "Choose a PDF"));
    }

    private async Task ShowPdfAsync(byte[] pdfBytes)
    {
        using var busy = _converting.AsToken();
        _error.Value = null;

        try
        {
            var documentId = Guid.NewGuid().ToString("n");
            var pages = new List<AssetUri>();

            // The document and each page hold native handles, so both are disposed
            using var document = PdfDocument.Load(pdfBytes);

            for (var i = 0; i < Math.Min(document.PageCount, MaxPages); i++)
            {
                using var page = document.GetPage(i);
                var (rgba, width, height) = page.GetPixels(PagePixels);

                using var jpeg = new MemoryStream();
                new ImageWriter().WriteJpg(rgba, width, height, ColorComponents.RedGreenBlueAlpha, jpeg, 85);

                var uri = new AssetUri(AssetClass.CloudFile, $"documents/{documentId}/page-{i + 1}.jpg", spaceId: app.GlobalState.SpaceId);
                await Asset.Instance.SetBytesAsync(uri, jpeg.ToArray(), new AssetMetadata(mimeType: "image/jpeg"));
                pages.Add(uri);
            }

            _pages.Value = pages;
        }
        catch (Exception ex) when (ex is InvalidDataException or UnauthorizedAccessException)
        {
            _error.Value = "That file is damaged or has a password, so it can't be shown.";
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Rendering a PDF of {pdfBytes.Length} bytes failed, the user can upload it again: {ex.Message}");
            _error.Value = "Couldn't open that PDF. Try again.";
        }
    }

    private void RenderViewer(UIView view)
    {
        view.Column([Card.Default, "gap-3 p-4 w-full max-w-2xl"], content: col =>
        {
            if (_converting.Value)
            {
                col.Text([Text.Caption], "Preparing the pages…");
            }

            if (_error.Value is { } error)
            {
                col.Text(["text-destructive text-sm"], error);
            }

            var pages = _pages.Value;

            if (pages.Count == 0)
            {
                col.EmptyState([EmptyState.RootSm], title: "No document open yet", description: "Open a PDF to read it here.");
                return;
            }

            // A list replaced by a shorter one can leave this client past its end
            var index = Math.Clamp(_pageIndex.Value, 0, pages.Count - 1);

            // Pinch, or Ctrl and scroll, zooms the page on the device; the key starts each page unzoomed
            col.PanZoom(["w-full h-[70vh] rounded-lg bg-white"], maxScale: 5, key: $"page-{index}", content: zoom =>
                zoom.Image(["w-full h-full object-contain"], assetUri: pages[index], alt: $"Page {index + 1} of {pages.Count}"));

            col.Row(["items-center justify-between gap-2"], content: row =>
            {
                row.Button([Button.OutlineMd], text: "Previous", disabled: index == 0, onClick: async () => _pageIndex.Value = index - 1);
                row.Text([Text.Caption], $"Page {index + 1} of {pages.Count}");
                row.Button([Button.OutlineMd], text: "Next", disabled: index == pages.Count - 1, onClick: async () => _pageIndex.Value = index + 1);
            });
        });
    }
    #endregion

    // Two pages of five-line staves with a title, written as a minimal PDF so the gallery needs no file
    private static byte[] SamplePdf() => SampleDocuments.Pdf(Enumerable.Range(1, 2).Select(number =>
    {
        var content = new StringBuilder($"BT /F1 22 Tf 72 730 Td (Sample score, page {number}) Tj ET\n0.8 w\n");

        for (var staff = 0; staff < 6; staff++)
        {
            for (var lineIndex = 0; lineIndex < 5; lineIndex++)
            {
                var y = 660 - staff * 100 - lineIndex * 8;
                content.Append($"72 {y} m 540 {y} l S\n");
            }
        }

        return content.ToString();
    }).ToArray());
}
