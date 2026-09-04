namespace Ikon.App.Patterns.Patterns;

// Pattern: background-processing-pipeline — see docs/patterns/background-processing-pipeline.md.
// The stubs outside the region stand in for the identity state, the case-file status writes and the
// per-stage extraction helpers the app owns; the docsnippet region is the canonical body the doc extracts.
internal sealed class BackgroundProcessingPipeline : IPatternDemo
{
    public string Slug => "background-processing-pipeline";
    public string Title => "Background processing pipeline";
    public string Category => "Data";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Backend pattern with no standalone UI: runs a multi-stage file pipeline (OCR, metadata, summary, classification) on a captured scope after the client disconnects. See the source and docs/patterns/background-processing-pipeline.md.");

    private readonly IAppBase app = null!;
    private readonly string _currentTenantId = "";
    private readonly string _currentUserId = "";
    private readonly Reactive<string> _backgroundTenantId = new("");
    private readonly Reactive<string> _backgroundUserId = new("");

    private string EffectiveTenantId => throw new NotImplementedException();

    private enum ProcessingStatus { Uploading, Queued, Processing, Done, Error }

    private sealed record FileMetadata(int PageCount, string? Author);

    private void RemoveActiveUpload(string uploadId) => throw new NotImplementedException();

    private Task UpdateCaseFileStatusAsync(
        Guid caseFileId, ProcessingStatus status, string? extractedText = null,
        FileMetadata? fileMetadata = null, string? errorMessage = null) => throw new NotImplementedException();

    private Task UpdateCaseDataForViewersAsync(Guid caseId) => throw new NotImplementedException();

    private Task<string?> ExtractTextWithOcrAsync(AssetUri assetUri) => throw new NotImplementedException();

    private Task<FileMetadata?> ExtractFileMetadataAsync(
        AssetUri assetUri, string fileName, string mimeType, long fileSize) => throw new NotImplementedException();

    private Task RunFileSummaryAsync(Guid caseFileId, Guid caseId, string extractedText) => throw new NotImplementedException();

    private Task RunFileClassificationAsync(Guid caseFileId, string tenantId) => throw new NotImplementedException();

    #region docsnippet:pattern-background-processing-pipeline
    private async Task HandleFileUploadCompleteAsync(
        Guid caseId, string uploadId, Guid caseFileId,
        string fileName, string mimeType, long fileSize, AssetUri assetUri, string hash)
    {
        // GetClientContext returns null once that client is gone; the background work still has to finish.
        var clientContext = app.GlobalState.GetClientContext(ReactiveScope.ClientId) ?? new Ikon.Common.Core.Protocol.Context();
        var capturedTenantId = _currentTenantId;
        var capturedUserId = _currentUserId;

        _ = Task.Run(async () =>
        {
            _backgroundTenantId.Value = capturedTenantId;
            _backgroundUserId.Value = capturedUserId;
            using var scope = ReactiveScope.Use(new UserScope(clientContext), new ClientScope(clientContext));
            await using var work = await app.BackgroundWork.StartAsync();

            try
            {
                await ProcessFileAsync(caseFileId, caseId, assetUri, fileName, mimeType, fileSize);
                RemoveActiveUpload(uploadId);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"File upload/processing failed: {ex.Message}");
                await UpdateCaseFileStatusAsync(caseFileId, ProcessingStatus.Error,
                    errorMessage: $"Processing failed: {ex.Message}");
                await UpdateCaseDataForViewersAsync(caseId);
                RemoveActiveUpload(uploadId);
            }
        });
    }

    private async Task ProcessFileAsync(Guid caseFileId, Guid caseId, AssetUri assetUri,
        string fileName, string mimeType, long fileSize)
    {
        await UpdateCaseFileStatusAsync(caseFileId, ProcessingStatus.Processing);
        await UpdateCaseDataForViewersAsync(caseId);

        var extractedText = await ExtractTextWithOcrAsync(assetUri);
        var fileMetadata = await ExtractFileMetadataAsync(assetUri, fileName, mimeType, fileSize);

        if (extractedText != null)
        {
            await RunFileSummaryAsync(caseFileId, caseId, extractedText);
        }

        await UpdateCaseFileStatusAsync(caseFileId, ProcessingStatus.Done, extractedText, fileMetadata);
        await RunFileClassificationAsync(caseFileId, EffectiveTenantId);
        await UpdateCaseDataForViewersAsync(caseId);
    }
    #endregion
}
