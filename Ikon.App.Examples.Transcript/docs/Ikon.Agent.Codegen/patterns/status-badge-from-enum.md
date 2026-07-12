<!-- mined-from: Sentrix -->
# Status Badge — Enum-to-Style Switch Expression

A pattern for turning a status enum into a coloured badge with one helper. The switch expression returns a `Badge.*` style array, the call site renders `Cell.Badge(label, style)` or `view.Text([..style], label)`. Adding a new enum case is one line in the switch.

## When to use

Anywhere your data has a small fixed status set — file processing, task status, severity, trust level, evidence strength. Avoid hand-writing the colour mapping at every call site (you'll drift) and avoid heavyweight component wrappers (overkill).

## Snippet

```csharp
private static string[] GetProcessingStatusStyle(ProcessingStatus status) => status switch
{
    ProcessingStatus.Uploading => [Badge.InfoSm],
    ProcessingStatus.Queued => [Badge.WarningSm],
    ProcessingStatus.Processing => [Badge.InfoSm],
    ProcessingStatus.Done => [Badge.SuccessSm],
    ProcessingStatus.Error => [Badge.ErrorSm],
    _ => [Badge.NeutralSm]
};

// Mid-state override: file is "Done" in the DB but classification still running.
// Surface it as a third visual state without adding a new enum value.
private static string[] GetProcessingStatusStyle(CaseFile file) =>
    file.ProcessingStatus == ProcessingStatus.Done && file.Classification == null
        ? [Badge.InfoSm]
        : GetProcessingStatusStyle(file.ProcessingStatus);

private string GetProcessingStatusLabel(CaseFile file) =>
    file.ProcessingStatus == ProcessingStatus.Done && file.Classification == null
        ? T("Classifying")
        : GetProcessingStatusLabel(file.ProcessingStatus);

private (string[] Style, string Label) GetEvidenceStrengthBucketDisplay(string? bucket) =>
    (bucket ?? "").Trim().ToLowerInvariant() switch
    {
        "forensicgrade" => (new[] { Badge.SuccessSm }, T("Forensic-grade")),
        "sourceoriginal" => (new[] { Badge.InfoSm }, T("Source-original")),
        "derivedcompiled" => (new[] { Badge.WarningSm }, T("Derived")),
        "weakscreenshot" => (new[] { Badge.ErrorSm }, T("Weak / screenshot")),
        "unknown" => (new[] { Badge.NeutralSm }, T("Unknown")),
        _ => (Array.Empty<string>(), "")
    };

// At the call site:
Cell.Badge(statusLabel, style: GetProcessingStatusStyle(file));
view.Text([Badge.NeutralSm], FormatDocumentKind(parsed.DocumentKind ?? "-"));
```

## Notes

- Switch expressions over enums + the platform's `Badge.InfoSm / WarningSm / SuccessSm / ErrorSm / GreySm / BrandSm` family. Don't invent new colours per app.
- For "in-between" states (e.g. file Done but classifier still running) overload by entity instead of adding enum values that don't exist in the DB schema. Two helpers: one by enum, one by entity that delegates.
- Pair a `GetXStyle` helper with a `GetXLabel` helper so the badge text and colour are always read from the same logical state.
- Return `(string[] Style, string Label)` tuples when the label is also conditional — easier than two parallel switches.
- Empty fallback `(Array.Empty<string>(), "")` lets the call site decide whether to render a badge or an em-dash.

## See also

- `state-machine-cards-and-transitions` — how badges and status changes integrate with state-driven cards.
