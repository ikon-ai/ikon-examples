namespace Ikon.App.Patterns.Patterns;

// Pattern: staged-file-download — see docs/patterns/staged-file-download.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class StagedFileDownload(IAppBase app) : IPatternDemo
{
    public string Slug => "staged-file-download";
    public string Title => "Offer a generated file without putting it in the UI tree";
    public string Category => "Web & data";
    public void RenderDemo(IView view) => Render(view);

    #region docsnippet:pattern-staged-file-download
    // The state the file is derived from, and the derivation itself. Both are inside the snippet:
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
                // expiresAt is what keeps staging from littering storage: the object deletes itself
                // even if the app never gets a chance to.
                new AssetMetadata(mimeType: "text/csv", expiresAt: DateTime.UtcNow.AddHours(6)),
                cancellationToken);

            // CloudFile is private, so this URL is signed and temporal — safe to hand to one client,
            // and useless to anyone who scrapes it later.
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
