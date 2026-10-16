using System.Globalization;
using System.Net;

namespace Ikon.App.Patterns.Patterns;

// Pattern: pdf-report — see docs/patterns/pdf-report.md.
// The example region below is the canonical body the doc extracts. Nothing is converted until the
// button is pressed, and the gallery runs FileConverter on its mock.
internal sealed class PdfReport(IAppBase app) : IPatternDemo
{
    public string Slug => "pdf-report";
    public string Title => "PDF report from app data";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    private IReadOnlyList<TimesheetRow> LoadRows() =>
    [
        new("Aino", "Website refresh", 12.5m),
        new("Mikael", "Website refresh", 7m),
        new("Aino", "Brand guide", 4.25m),
    ];

    #region example:pattern-pdf-report
    private sealed record TimesheetRow(string Person, string Project, decimal Hours);

    private readonly string _exportKey = Guid.NewGuid().ToString("n");
    private readonly Reactive<bool> _busy = new(false);
    private readonly Reactive<string?> _error = new(null);

    /// <summary>
    /// The converter receives only these bytes, so the HTML carries everything it needs: styles in
    /// a style block, images as data: URIs, no links to the app. Every value is encoded, because
    /// it is user data going into markup.
    /// </summary>
    private static string BuildReportHtml(string title, IReadOnlyList<TimesheetRow> rows)
    {
        var html = new StringBuilder()
            .Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>")
            .Append("body{font-family:sans-serif;font-size:12px}table{border-collapse:collapse;width:100%}")
            .Append("th,td{border:1px solid #ccc;padding:4px 8px;text-align:left}.num{text-align:right}")
            .Append("</style></head><body><h1>").Append(WebUtility.HtmlEncode(title)).Append("</h1>")
            .Append("<table><tr><th>Person</th><th>Project</th><th class=\"num\">Hours</th></tr>");

        foreach (var row in rows)
        {
            html.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.Person))
                .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Project))
                .Append("</td><td class=\"num\">").Append(row.Hours.ToString("0.00", CultureInfo.InvariantCulture)).Append("</td></tr>");
        }

        return html.Append("<tr><th colspan=\"2\">Total</th><th class=\"num\">")
            .Append(rows.Sum(row => row.Hours).ToString("0.00", CultureInfo.InvariantCulture))
            .Append("</th></tr></table></body></html>")
            .ToString();
    }

    /// <summary>
    /// Runs on the click, never on render: each conversion costs credits. The PDF is staged in
    /// private storage under one path per session and expires on its own.
    /// </summary>
    private async Task<string?> StageReportPdfAsync()
    {
        using var busy = _busy.AsToken();
        _error.Value = null;

        try
        {
            var html = BuildReportHtml("Timesheet", LoadRows());
            // The .html extension is what tells the converter the bytes are a web page.
            var pdf = await FileConverter.ConvertToPdfAsync(Encoding.UTF8.GetBytes(html), "timesheet.html");

            var uri = new AssetUri(AssetClass.CloudFile, $"exports/{_exportKey}/timesheet.pdf", spaceId: app.GlobalState.SpaceId);
            await Asset.Instance.SetBytesAsync(uri, await pdf.GetDataAsync(),
                new AssetMetadata(mimeType: "application/pdf", expiresAt: DateTime.UtcNow.AddHours(6)));

            return (await Asset.Instance.GetMetadataAsync(uri)).Url;
        }
        catch (Exception ex) when (ex is AIException or HttpRequestException or InvalidDataException)
        {
            // Returning null cancels the download; the message tells the user to try again.
            _error.Value = "Couldn't create the PDF. Try again.";
            return null;
        }
    }

    private void Render(IView view)
    {
        view.Column(["gap-2 items-start"], content: column =>
        {
            column.ActionButton([Button.OutlineMd],
                action: ActionKind.DownloadFile,
                options: new DownloadFileActionOptions { FileName = "timesheet.pdf", UrlProvider = StageReportPdfAsync },
                disabled: _busy.Value,
                text: _busy.Value ? "Creating PDF…" : "Download PDF");

            if (_error.Value is { } error)
            {
                column.Text(["text-destructive text-sm"], text: error);
            }
        });
    }
    #endregion
}
