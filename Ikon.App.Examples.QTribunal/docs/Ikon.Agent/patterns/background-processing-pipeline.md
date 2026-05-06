<!-- mined-from: Sentrix -->
# Background Processing Pipeline — Fire-and-Forget After Upload Returns

After a synchronous upload (or any user-facing action) completes, kick off the heavy processing on a background `Task.Run`. The user gets immediate UI feedback ("Uploaded!"); the multi-step pipeline (extract text → metadata → classify → summarise → audit) runs detached, updating row status as each stage finishes.

## When to use

Whenever the work after an action is multi-second and benefits from progressive status updates rather than blocking the user. File processing chains, batch enrichment, multi-step extraction, post-import cleanup.

## Snippet

```csharp
private async Task HandleFileUploadCompleteAsync(
    Guid caseId, string uploadId, Guid caseFileId,
    string fileName, string mimeType, long fileSize, AssetUri assetUri, string hash)
{
    var clientContext = app.GlobalState.GetClientContext(ReactiveScope.ClientId);
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
```

## Notes

- `_ = Task.Run(async () => ...)` — discard the task so the originating handler returns immediately. Don't `await` it; the user just sees the row appear in the table.
- **Capture identity locals before crossing the thread boundary**: `capturedTenantId`, `capturedUserId`, the `clientContext`. Once you're in a `Task.Run`, the `ReactiveScope.ClientId` and any AsyncLocal context from the request thread are gone. Re-establish with `ReactiveScope.Use(new UserScope(...), new ClientScope(...))`.
- `app.BackgroundWork.StartAsync()` is the platform's way to keep the app alive while clients are disconnected — without it the work can be killed when the last client leaves.
- Update a status enum on the entity (`Uploading → Processing → Done | Error`) and re-broadcast (`UpdateCaseDataForViewersAsync`) at every stage. The UI reads from a `Reactive<List<>>` of these entities and re-renders the row badge automatically.
- Always wrap the inner body in try/catch — uncaught background exceptions vanish into the void.

## See also

- `file-upload-with-progress` — the upload front half this pipeline runs after.
- `status-badge-from-enum` — how to render the status changes the pipeline emits.
