<!-- mined-from: Ikon.App.Patterns -->
# Shareable Result Export — PNG By Screenshot, PDF By Conversion

They are two different services and the instinct to reach for one of them twice is the trap. An
image of a result comes from `WebScraper.TakeScreenshotAsync` pointed at a page the app itself
serves; a PDF comes from `FileConverter.ConvertToPdfAsync`, which takes a `Url`, `Data` or
`AssetUri` and returns a `ConvertedFile`.

Neither renders the app's own view tree — both need a reachable URL, so the export target is a
public route the app publishes, not the live session.

## When to use

"Share my results", "download as PDF", "save this chart as an image", a certificate, a receipt, an
end-of-game summary card.

## Notes

- `ScreenshotConfig.FullPage` captures past the viewport; `Width`/`Height` set the shot size.
  `JavaScript` runs in the page before the capture — the hook for stripping controls that should
  not appear in a shared image.
- `ScreenshotResult.Data` is non-null. `ConvertedFile.Data` is **nullable**, because a conversion
  can arrive as a `Url` instead (`ResultDelivery`) — check before using it.
- `WebScraperModel.LocalPlaywright` screenshots in-process; the hosted models are for scraping
  pages you do not serve. Check `SupportsScreenshotting` if the model is configurable.
- Hand the bytes to `ActionKind.DownloadFile` with `DownloadFileActionOptions { Filename, Data }`.
  Download is declarative and client-side — never a server round-trip at click time.
- For a chart or card you fully control, rendering SVG in C# and showing it with `view.Image` is
  cheaper than a screenshot and needs no browser — see `server-side-svg-visual`.

## Snippet

```csharp
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
```

## See also

- `copy-and-share-action-row` — the surrounding row of copy / share affordances.
- `server-side-svg-visual` — build the image in C# instead of screenshotting a page.
