namespace Ikon.App.Patterns.Patterns;

// Pattern: staged-file-download — see docs/patterns/staged-file-download.md.
// The example region below is the canonical body the doc extracts. The demo starts staging on the
// first press, never from a render, and each later press edits the report so the debounce restages it.
internal sealed class StagedFileDownload(IAppBase app) : IPatternDemo
{
    public string Slug => "staged-file-download";
    public string Title => "Offer a generated file without putting it in the UI tree";
    public string Category => "Web & data";

    private static readonly string[] DemoRows =
    [
        "2026-09-01,Helsinki,Oat latte,412",
        "2026-09-01,Tampere,Cinnamon bun,288",
        "2026-09-02,Helsinki,Filter coffee,530",
        "2026-09-02,Turku,Rye sandwich,174",
        "2026-09-03,Tampere,Oat latte,366",
    ];

    private int _demoRowCount;

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Row(["gap-2 items-center"], content: row =>
            {
                row.Button([Button.OutlineSm], text: _stageEffect is null ? "Build the sales report" : "Add a day to the report",
                    onClick: async () =>
                    {
                        _demoRowCount = Math.Min(_demoRowCount == 0 ? 2 : _demoRowCount + 1, DemoRows.Length);
                        _report.Value = "date,store,product,units\n" + string.Join("\n", DemoRows.Take(_demoRowCount));

                        if (_stageEffect is null)
                        {
                            StartStaging();
                        }
                    });
                Render(row);
            });
            col.Text(["text-xs text-zinc-400"], _report.Value.Length == 0
                ? "No report yet"
                : $"report.csv has {_demoRowCount} rows; it is uploaded 1.5 s after the last change");
        });
    }

    #region example:pattern-staged-file-download
    // The state the file is derived from, and the derivation itself. Both are inside the example:
    // the corpus check compiles the extracted region on its own, so anything the code names has to
    // be in it.
    private readonly Reactive<string> _report = new("");

    private byte[] BuildReport() => Encoding.UTF8.GetBytes(_report.Value);

    // Only the URL crosses the wire. The bytes go to PRIVATE cloud storage and the browser fetches
    // them from there, so a large artefact never enters the reactive tree and never re-diffs.
    private readonly Reactive<string?> _downloadUrl = new(null);
    private readonly string _exportKey = Guid.NewGuid().ToString("n");
    private ReactiveEffect? _stageEffect;

    private AssetUri ExportUri => new(AssetClass.CloudFile, $"exports/{_exportKey}/report.csv", spaceId: app.GlobalState.SpaceId);

    /// <summary>
    /// Stage the file whenever the state it is derived from changes. ReactiveEffect cancels the
    /// token when a dependency changes again, so the delay debounces a burst of edits into one
    /// upload instead of one per keystroke.
    /// </summary>
    private void StartStaging()
    {
        _stageEffect = new ReactiveEffect(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), cancellationToken);

            await Asset.Instance.SetBytesAsync(
                ExportUri,
                BuildReport(),
                // expiresAt is what keeps staging from littering storage: the platform removes the
                // object, best-effort, once it passes without an update, even if the app never gets
                // a chance to.
                new AssetMetadata(mimeType: "text/csv", expiresAt: DateTime.UtcNow.AddHours(6)),
                cancellationToken);

            // CloudFile is private, so this URL is signed and valid for one hour — safe to hand to one
            // client, and useless to anyone who scrapes it later. It is fetched again only when the
            // report changes, so a page left idle past the hour needs it re-resolved (UrlProvider
            // resolves it on each click instead).
            _downloadUrl.Value = (await Asset.Instance.GetMetadataAsync(ExportUri)).Url;
        }, _report);

        app.OnStopping(async () => await Asset.Instance.DeleteAsync(ExportUri));
    }

    /// <summary>
    /// A link, not a download action: the href is a string, so the UI diff stays tiny however large
    /// the file is.
    /// </summary>
    private void Render(IView view)
    {
        view.Link(
            [Button.OutlineMd, Button.IconLeft, _downloadUrl.Value == null ? "opacity-50 pointer-events-none" : ""],
            href: _downloadUrl.Value ?? "#",
            target: "_blank",
            content: v =>
            {
                v.Icon([Icon.Default], name: "download");
                v.Text(text: _downloadUrl.Value == null ? "Preparing..." : "Download");
            });
    }
    #endregion
}
