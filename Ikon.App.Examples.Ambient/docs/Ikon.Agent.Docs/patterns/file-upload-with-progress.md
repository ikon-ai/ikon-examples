<!-- mined-from: Sentrix -->
# File Upload with Progress — Drag-Drop Zone + Live Progress Tracker

A drag-and-drop upload zone wired to a `ReactiveList` of in-flight uploads. Per-file callbacks (`PreStart`, `Start`, `Progress`, `Complete`, `Error`) update a tracker so the UI can show live percentages, dedupe by hash, and surface error toasts.

## When to use

Any app that lets users upload one or more files and needs visible progress, server-side dedup, or per-file processing afterwards. The shape works identically for evidence files, attachments, image sets, audio clips.

## Snippet

```csharp
private sealed class UploadTracker
{
    public string UploadId = "";
    public string FileName = "";
    public double Progress;
    public string Hash = "";
    public Guid? CaseFileId = null;
}

private readonly ReactiveList<UploadTracker> _activeUploads = new();
private readonly Reactive<string?> _fileUploadError = new(null);

private void RenderFileUploadArea(UIView view, Guid caseId)
{
    view.FileUpload(
        multiple: true,
        style: SentrixStyles.UploadZone,
        onUploadPreStart: args => OnFileUploadPreStartAsync(args),
        onUploadStart: args => OnFileUploadStartAsync(caseId, args),
        onUploadProgress: args => OnFileUploadProgressAsync(args),
        onUploadComplete: args => OnFileUploadCompleteAsync(caseId, args),
        onUploadError: args => OnFileUploadErrorAsync(caseId, args),
        content: view =>
        {
            view.Column(["items-center gap-2"], content: view =>
            {
                view.Icon([Icon.Default, "text-brand-primary w-8 h-8"], name: "cloud-upload");
                view.Text(["text-sm font-medium text-foreground"], T("Upload Evidence Files"));
                view.Text(["text-xs text-muted-foreground"], T("Drag and drop or click to browse"));
            });
        });
}

private async Task<FileUploadResult> OnFileUploadPreStartAsync(FileUploadPreStartArgs args)
{
    if (args.Size > MaxFileSizeBytes)
    {
        _fileUploadError.Value = T("File too large. Maximum size is 2000 MB");
        return false;
    }

    _activeUploads.Add(new UploadTracker { UploadId = args.UploadId, FileName = args.FileName });
    return true;
}

private Task OnFileUploadProgressAsync(FileUploadProgressArgs args)
{
    var tracker = _activeUploads.FirstOrDefault(u => u.UploadId == args.UploadId);
    if (tracker != null)
    {
        tracker.Progress = args.ProgressPercentage;
        _activeUploads.NotifyUpdate();
    }
    return Task.CompletedTask;
}
```

## Notes

- `FileUpload` exposes the full lifecycle as five callbacks. `PreStart` returns `false` to reject (size limit, type, etc.); `Start` returns a `FileUploadResult { AssetUri = uri }` so the bytes stream straight to storage. That `AssetUri` — on the way in and on the way back out as `args.AssetUri` (an `AssetUri?`) in `onUploadComplete` — is the struct every `Asset.Instance.*` call takes: null-check it and pass it on, no parsing.
- Rows live in a `ReactiveList<UploadTracker>` — `_activeUploads.Add(tracker)` on PreStart notifies once; enumeration and LINQ (`FirstOrDefault`) run straight on the reactive.
- `UploadTracker` is a mutable POCO INSIDE the list, so a field write is invisible to the list's mutators. Mutate the field, then call `_activeUploads.NotifyUpdate()` — the escape hatch for in-place item edits. Every `ReactiveList` mutator copies the list, and re-copying it on each progress tick is wasteful at 60 fps.
- Match `args.UploadId` to find the row; never index by filename (collisions on duplicate names).
- Surface upload errors via a `Toast` bound to a separate `Reactive<string?>`; reset to `null` on `onOpenChange`.
- Render in-flight rows alongside completed rows in the same `DataTable` so users see "Uploading 47%" inline with finished items.

## See also

- `busy-flag-loading` — single-flag loading state when full progress isn't needed.
- `background-processing-pipeline` — what to do once the upload completes (background processing chain).
