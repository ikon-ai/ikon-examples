<!-- mined-from: Sentrix -->
# Weighted Progress Banner — Two Phases, One Smooth Bar

A persistent top banner showing overall completion across two pipeline phases (per-file intake + case-level preprocessing) as a single percentage. Each phase contributes a fixed weight; the bar advances smoothly across the phase boundary instead of jumping back to 0% when phase 2 starts.

## When to use

Any multi-phase async pipeline where the user wants one number for "how done are we?" — file ingestion + extraction, build + deploy, render + post-process, scrape + analyse. Avoids the surprise of a bar that hits 100% then restarts.

## Snippet

```csharp
private void RenderCaseProcessingBanner(UIView view, Case caseEntity)
{
    _caseProcessingInfo.Value.TryGetValue(caseEntity.Id, out var caseInfo);

    var filesTotal = _selectedCaseFiles.Value.Count;
    var filesInProgress = _selectedCaseFiles.Value.Count(f =>
        f.ProcessingStatus == ProcessingStatus.Uploading ||
        f.ProcessingStatus == ProcessingStatus.Queued ||
        f.ProcessingStatus == ProcessingStatus.Processing ||
        (f.ProcessingStatus == ProcessingStatus.Done && f.Classification == null));
    var filesDone = _selectedCaseFiles.Value.Count(IsCaseFileFullyProcessed);

    if (caseInfo == null && filesInProgress == 0)
    {
        return;
    }

    // File intake is the first 10% of "analysis"; case-level preprocessing is the
    // remaining 90%. Fixed weights keep the bar smooth across phases instead of
    // restarting at the boundary.
    const int fileWeight = 10;
    const int preprocessWeight = 90;

    var filePct = filesTotal > 0
        ? (filesDone / (double)filesTotal) * fileWeight
        : fileWeight;
    var preprocessPct = 0.0;

    if (caseInfo != null && caseInfo.StepsTotal > 0)
    {
        preprocessPct = (caseInfo.StepsCompleted / (double)caseInfo.StepsTotal) * preprocessWeight;
    }

    // Clamp to 1..99: never 0 (looks stalled) and never 100 (implies done, but
    // the banner only disappears once the case-processing info entry is cleared).
    var totalPct = (int)Math.Round(filePct + preprocessPct);
    totalPct = Math.Clamp(totalPct, 1, 99);

    view.Row(["items-center gap-3 px-4 py-2 border-b border-secondary bg-accent/30 shrink-0"], content: view =>
    {
        view.Icon([Icon.Spinner, "w-4 h-4"], name: "loader");
        view.Text(["text-sm font-medium text-foreground"], T("SENTRIX is analyzing this case"));
        view.Text(["text-xs text-muted-foreground"], TF("{0}% complete", totalPct));
    });
}
```

## Notes

- Weights are constants (`fileWeight = 10`, `preprocessWeight = 90`) summing to 100. To change the perceived split, just edit the constants.
- `Math.Clamp(totalPct, 1, 99)` prevents two visual lies: 0% looks stalled (and would render an empty bar), 100% looks finished (when in reality the banner is dismissed by a separate "everything cleared" signal).
- The banner returns early when nothing's in flight — no work, no banner.
- `IsCaseFileFullyProcessed(file)` deliberately requires `Done && Classification != null` — files that finished text extraction but are still being classified shouldn't tick up the file-phase percentage prematurely.
- `bg-accent/30 shrink-0` makes the banner visually present but unobtrusive; `shrink-0` stops it from being squeezed away in a flex column.

## See also

- `background-processing-pipeline` — what's running while the banner shows.
- `score-bar-meter` — single-state percentage bar without the phase logic.
