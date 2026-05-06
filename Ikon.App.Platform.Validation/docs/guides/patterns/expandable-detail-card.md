<!-- mined-from: NoBrainer -->
# Expandable Detail Card — Click Header To Reveal Markdown

A card whose header (icon + title + subtitle + chevron) is itself a button. Clicking it toggles the artifact's id in a `ClientReactive<HashSet<string>>` of expanded ids; expanded cards render a Markdown body (or syntax-styled code) below the header inside the same rounded outline.

## When to use

Showing a list of mixed-type artifacts (plans, code, notes, attachments), search hits, log entries, version history — anywhere the user wants to scan headers and then drill into one or two without leaving the list. The HashSet keying lets multiple cards stay open at once, unlike a Radix Accordion which is single-open by default.

## Snippet

```csharp
private void RenderArtifactCard(UIView view, (ThreadArtifact Artifact, string SourceThread) item)
{
    var artifact = item.Artifact;
    var isExpanded = _expandedArtifacts.Value.Contains(artifact.Id);

    var typeIcon = artifact.Type switch
    {
        "code" => "file-code",
        "plan" => "list-checks",
        "skill" => "sparkles",
        _ => "file-text"
    };

    view.Box(["rounded-lg bg-black/[0.02] border border-black/[0.04] overflow-hidden"], content: view =>
    {
        view.Button(["w-full text-left bg-transparent hover:bg-black/[0.02] px-3 py-2.5 border-0 transition-colors duration-150"],
            onClick: async () => ToggleArtifactExpanded(artifact.Id),
            content: view =>
            {
                view.Row(["items-center gap-2"], content: view =>
                {
                    view.Icon(["w-4 h-4 text-black/20"], name: typeIcon);
                    view.Column(["flex-1 gap-0 min-w-0"], content: view =>
                    {
                        view.Text(["text-sm text-black/50 font-medium truncate"], artifact.Name);
                        view.Text(["text-[10px] text-black/15"], item.SourceThread);
                    });
                    view.Icon(["w-3.5 h-3.5 text-black/15"],
                        name: isExpanded ? "chevron-up" : "chevron-down");
                });
            });

        if (isExpanded && !string.IsNullOrWhiteSpace(artifact.Content))
        {
            view.Box(["px-3 pb-3 border-t border-black/[0.04]"], content: view =>
            {
                if (artifact.Type == "code")
                {
                    view.Box(["mt-2 bg-black/[0.03] rounded p-3 overflow-x-auto"], content: view =>
                    {
                        view.Text(["text-xs text-black/50 font-mono whitespace-pre-wrap break-all leading-relaxed"], artifact.Content);
                    });
                }
                else
                {
                    view.Markdown(
                        style: ["mt-2 text-xs text-black/40 leading-relaxed",
                            "[&_h1]:text-sm [&_h1]:font-medium",
                            "[&_ul]:pl-4 [&_li]:mb-0.5"],
                        content: artifact.Content);
                }
            });
        }
    });
}

private void ToggleArtifactExpanded(string artifactId)
{
    var set = new HashSet<string>(_expandedArtifacts.Value);
    if (set.Contains(artifactId)) set.Remove(artifactId);
    else set.Add(artifactId);
    _expandedArtifacts.Value = set;
}
```

## Notes

- Always copy-into-new-set in the toggle (`new HashSet<string>(_expandedArtifacts.Value)`) before mutating; mutating the existing set in place won't trigger the reactive update.
- Use `ClientReactive<HashSet<string>>` so each viewer can expand independently — expansion is presentational, not collaborative.
- The card border is `border border-black/[0.04]` and the divider on expand is `border-t` of the same color, preserving the outer ring.
- `[&_h1]:text-sm` etc. inline arbitrary-value selectors style nested Markdown without needing a separate prose plugin — keeps the card compact.

## See also

- `markdown-prose-style-tokens` — reusable nested-element styling for Markdown blocks
- `accordion-section` — single-open variant with the Radix accordion primitive
