<!-- mined-from: Sentrix -->
# AI Suggestion Banner — Confirm or Dismiss Inline

When the app heuristically detects something it thinks the user wants (here: an uploaded file matching a previously-flagged "missing" reference), it surfaces a banner inline with `Confirm` and `Dismiss` buttons. Confirm performs the side-effect; Dismiss just records that the user said no, so the banner doesn't keep coming back.

## When to use

Anywhere you can heuristically infer a probable user intent but aren't confident enough to auto-apply it — auto-link suggestions, duplicate detection, "did you mean to add X?". The banner is non-modal; if the user ignores it, the rest of the page still works.

## Snippet

```csharp
private readonly Reactive<HashSet<string>> _dismissedMissingFileMappings = new(new HashSet<string>());

private void RenderPendingMissingFileMappings(UIView view)
{
    var mappings = GetPotentialMissingFileMappings();
    if (mappings.Count == 0) return;

    view.Column(["gap-2 px-4 py-3 border border-info bg-info/5 rounded-md"], content: view =>
    {
        view.Text(["text-sm font-semibold text-primary"], T("Confirm missing-document mappings"));
        view.Text(["text-xs text-tertiary"], T("Sentrix thinks these recent uploads match documents that were previously marked missing. Confirm to link them, or dismiss to keep the card open."));

        foreach (var (uploaded, missing) in mappings)
        {
            view.Row(["flex items-center gap-3 bg-background border border-secondary rounded-md px-3 py-2"], content: view =>
            {
                view.Column(["flex-1 min-w-0 gap-0.5"], content: view =>
                {
                    view.Text(["text-sm font-medium text-primary truncate"], uploaded.Name);
                    view.Text(["text-xs text-tertiary truncate"], TF("Missing: {0}", missing.Label));
                });

                view.Button([Button.OutlineSm], label: T("Dismiss"),
                    onClick: async () => DismissMissingFileMapping(uploaded.Id, missing.Id));

                view.Button([Button.PrimarySm], label: T("Confirm"),
                    onClick: async () => await ConfirmMissingFileMappingAsync(uploaded.Id, missing.Id));
            });
        }
    });
}

private void DismissMissingFileMapping(Guid uploadedFileId, Guid missingRefId)
{
    var key = $"{uploadedFileId}:{missingRefId}";
    var updated = new HashSet<string>(_dismissedMissingFileMappings.Value) { key };
    _dismissedMissingFileMappings.Value = updated;
}

private async Task ConfirmMissingFileMappingAsync(Guid uploadedFileId, Guid missingRefId)
{
    await using var db = CreateScopedDbContext();
    var missing = await db.CaseMissingFileRefs.FindAsync(missingRefId);
    if (missing != null)
    {
        missing.LinkedUploadedFileId = uploadedFileId;
        missing.Status = MissingFileRefStatus.Completed;
        await db.SaveChangesAsync();
    }
}
```

## Notes

- `_dismissedMissingFileMappings` is a `HashSet<string>` of `"uploadedId:missingId"` keys. Suggestion code filters out any pair whose key is in the set, so dismissed pairs disappear permanently for this session without writing to the DB.
- Confirm performs the actual mutation (link records, mark Completed) and lets normal data refresh remove the banner. Don't manually remove it — let the suggestion-source query naturally return empty.
- The inference function (`GetPotentialMissingFileMappings`) does fuzzy name matching (`NormalizeForMatch` strips punctuation, lowercases, then `Contains` either direction). Heuristic is intentional — false positives are cheap because the user just clicks Dismiss.
- Banner uses `bg-info/5` (5% info tint) so it reads as informational, not alarming. Severity bumps would be `bg-warning/5` or `bg-error/5`.
- Dismiss state is in-memory; persist to DB if it should survive page refresh.

## See also

- `ai-prefill-form-from-description` — proactive prefill at form start.
- `destructive-confirm-dialog` — confirm pattern when the action is irreversible.
