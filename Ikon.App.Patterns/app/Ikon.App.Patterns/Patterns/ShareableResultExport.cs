namespace Ikon.App.Patterns.Patterns;

// Pattern: shareable-result-export — see docs/patterns/shareable-result-export.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class ShareableResultExport : IPatternDemo
{
    public string Slug => "shareable-result-export";
    public string Title => "Shareable image or PDF of a result";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    private string PublicResultUrl() => throw new NotImplementedException();

    #region docsnippet:pattern-shareable-result-export
    private readonly Reactive<byte[]?> _png = new(null);
    private readonly Reactive<byte[]?> _pdf = new(null);
    private readonly Reactive<bool> _busy = new(false);

    /// <summary>
    /// Screenshot a page the app itself serves. FullPage captures past the viewport, and
    /// JavaScript runs in the page before the shot -- the hook for hiding chrome that should not
    /// appear in a shared image.
    /// </summary>
    private async Task RenderPngAsync(string url)
    {
        using var scraper = new WebScraper(WebScraperModel.LocalPlaywright);

        var shot = await scraper.TakeScreenshotAsync(new ScreenshotConfig
        {
            Url = url,
            FullPage = true,
            Width = 1200,
            JavaScript = "document.querySelectorAll('.no-export').forEach(e => e.remove());",
        });

        _png.Value = shot.Data;
    }

    /// <summary>
    /// PDF is a conversion, not a screenshot: hand FileConverter a Url, Data or AssetUri and it
    /// returns a ConvertedFile. Data is nullable there because the result can arrive as a URL.
    /// </summary>
    private async Task RenderPdfAsync(string url)
    {
        using var converter = new FileConverter(FileConverterModel.ConvertApi.ToString());

        var file = await converter.ConvertToPdfAsync(new FileConverterConfig
        {
            Url = url,
            FileName = "results.pdf",
        });

        _pdf.Value = file.Data;
    }

    private async Task ExportAsync()
    {
        if (_busy.Value)
        {
            return;
        }

        using var _ = _busy.AsToken();
        var url = PublicResultUrl();

        try
        {
            await RenderPngAsync(url);
            await RenderPdfAsync(url);
        }
        catch (AIException)
        {
            // Export failed; the results themselves are untouched and the user can retry.
        }
    }

    private void Render(IView view)
    {
        view.Row(["gap-2"], content: row =>
        {
            row.Button(
                disabled: _busy.Value,
                onClick: ExportAsync,
                content: v => v.Text(text: _busy.Value ? "Preparing…" : "Prepare export"));

            // Download is a declarative ActionButton, never a server call -- the bytes are already
            // on the client by the time the button renders.
            if (_png.Value is { } png)
            {
                row.ActionButton(
                    action: ActionKind.DownloadFile,
                    options: new DownloadFileActionOptions { Filename = "results.png", Data = png },
                    content: v => v.Text(text: "PNG"));
            }

            if (_pdf.Value is { } pdf)
            {
                row.ActionButton(
                    action: ActionKind.DownloadFile,
                    options: new DownloadFileActionOptions { Filename = "results.pdf", Data = pdf },
                    content: v => v.Text(text: "PDF"));
            }
        });
    }
    #endregion
}
