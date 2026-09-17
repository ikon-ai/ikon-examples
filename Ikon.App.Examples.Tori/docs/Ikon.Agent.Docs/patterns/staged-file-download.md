<!-- mined-from: Ikon.App.Baseplate -->
# Staged File Download — Private Asset, Signed URL, Self-Deleting
<!-- checked-against: 25677e0930569ff0 -->
`DownloadFileActionOptions.Data` takes its bytes **eagerly** — the options are built on every render
of the control — so a download button in a header re-runs the whole export on every state change,
including every drag tick of an editor.

Until 2026-09, it was worse than wasted CPU: `Data` is a `byte[]`, `System.Text.Json` writes those as
base64, and the options record was serialised into the node's `actionOptionsJson` with `Data` still
set. The payload conversion rescued only `Url`, so **the entire file rode in the UI diff as base64 on
every render**. `Data` now registers as a content-keyed payload and is stripped before serialisation
(`DownloadActionPayloadTests` guards it), so on a current library the bytes no longer reach the tree.

The eager rebuild remains, and that is reason enough here: for an artefact that is large or
re-derived while the user works, stage it in private cloud storage and put only the **URL** in the
tree.

## When to use

Any generated artefact a person downloads and that changes while they work: an export, a report, a
place file, a rendered document, a data dump. Anything over a few kilobytes, or anything derived from
state that changes often.

`ActionKind.DownloadFile` remains right for a *small, finished* result produced once on demand — a
receipt, a QR code, a summary card. The moment the artefact is large or re-derived, stage it.

## Notes

- **`AssetClass.CloudFile` is private; `CloudFilePublic` is not.** Use the private class and let
  `GetMetadataAsync(uri).Url` hand back a signed, temporal URL. Never publish a per-user artefact to
  a public path just to make a link work.
- **`expiresAt` on `AssetMetadata` is what stops staging from littering storage.** The object deletes
  itself even if the app crashes before it can clean up. Set it; an unbounded pile of stale exports
  is the failure mode of this pattern.
- **Delete on `OnStopping` as well.** Belt and braces — the expiry is the guarantee, the delete is
  the courtesy.
- **One stable path per session, overwritten**, not a fresh name per export. A new key per click
  multiplies objects that all have to expire independently.
- **`ReactiveEffect` is the debounce.** It re-runs when a dependency changes and cancels the token
  when the next change arrives, so a `Task.Delay` at the top collapses a burst of edits into a single
  upload. Catch `OperationCanceledException` and do nothing — a superseded export is not an error.
- **Render a `view.Link`, not a button.** The href is a string, so the diff stays tiny regardless of
  file size, and the browser gets ordinary download semantics.
- On a current library the cost is CPU, not bandwidth — payloads are content-keyed, so an unchanged
  export diffs to nothing. On an older one the file itself was in the diff. Either way, stage
  anything expensive to build or frequently re-derived.
- Do **not** reach for a custom `[HttpGet]` for this. It puts an auth surface in your app that the
  asset system already solves, and it makes your app the thing that serves the bytes — every download
  crossing your own endpoint instead of coming straight from storage. `HttpResult.Bytes(body,
  contentType)` does write a `byte[]` body through unchanged; every other `HttpResult` factory
  base64-encodes a `byte[]` inside JSON. So an endpoint CAN serve a binary file; the reason not to is
  the auth surface and the traffic, and a staged asset needs no endpoint.

## The one-line form

When the artefact only exists to be downloaded, skip the staging bookkeeping and let the action do
it. `UrlProvider` runs **on the click**, on the server, and only the address it returns crosses the
transport — the browser fetches the file from wherever it is hosted:

```csharp
// Nothing in here runs until the button is pressed: the callback is invoked on the server by the
// click, and only the address it returns crosses the transport.
async Task<string?> StageReportAsync()
{
    // ONE STABLE PATH, overwritten on every export — see the note above. A fresh Guid per click
    // leaves a pile of objects each expiring on its own schedule. The uri is already scoped to the
    // space, so name the artefact rather than the occasion.
    var uri = new AssetUri(
        AssetClass.CloudFile, "exports/report.csv", spaceId: app.GlobalState.SpaceId);

    await Asset.Instance.SetBytesAsync(
        uri,
        Encoding.UTF8.GetBytes("id,total\n1,42\n"),
        new AssetMetadata(mimeType: "text/csv", expiresAt: DateTime.UtcNow.AddHours(6)));

    return (await Asset.Instance.GetMetadataAsync(uri)).Url;
}

view.ActionButton([Button.OutlineMd],
    action: ActionKind.DownloadFile,
    options: new DownloadFileActionOptions
    {
        Filename = "report.csv",
        UrlProvider = StageReportAsync
    },
    text: "Download");
```

`AssetProvider` is the same thing keyed by `AssetUri`, resolved to its signed URL for you — the
mirror of an upload that streams into asset storage through `onUploadStart`. Bytes go in one way and
come back out the other, and never through the reactive channel in either direction.

Reach for the explicit staging below when the URL is also shown elsewhere, shared, or wanted before
anyone clicks.

## Snippet

```csharp
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
```
