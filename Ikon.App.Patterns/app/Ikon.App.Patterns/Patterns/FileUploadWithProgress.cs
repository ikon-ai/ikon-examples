namespace Ikon.App.Patterns.Patterns;

// Pattern: file-upload-with-progress — see docs/patterns/file-upload-with-progress.md.
// The stubs outside the region stand in for the app's size limit, style tokens, localization helper,
// and the completion/error hooks so the drop-zone and progress-tracker body the doc extracts compiles.
internal sealed class FileUploadWithProgress : IPatternDemo
{
    public string Slug => "file-upload-with-progress";
    public string Title => "File upload with progress";
    public string Category => "Media";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "UI pattern whose demo needs case context to render: a drop-zone streams uploads while a per-file tracker follows each upload's progress. See the source and docs/patterns/file-upload-with-progress.md.");

    private const long MaxFileSizeBytes = 2_000_000_000;

    private static class SentrixStyles
    {
        public static readonly string[] UploadZone = ["p-6 border border-dashed rounded-xl"];
    }

    private static string T(string key) => throw new NotImplementedException();
    private Task<FileUploadResult> OnFileUploadStartAsync(Guid caseId, FileUploadStartArgs args) => throw new NotImplementedException();
    private Task OnFileUploadCompleteAsync(Guid caseId, FileUploadCompleteArgs args) => throw new NotImplementedException();
    private Task OnFileUploadErrorAsync(Guid caseId, FileUploadErrorArgs args) => throw new NotImplementedException();

    #region docsnippet:pattern-file-upload-with-progress
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
    #endregion
}
