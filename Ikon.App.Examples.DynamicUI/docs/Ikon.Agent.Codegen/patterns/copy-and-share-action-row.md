<!-- mined-from: Tori -->
# Copy And Share Action Row — Floating Affordance Over A Read-Only Block

A small row of icon buttons floats in the bottom-right of a read-only content panel (transcript, summary, generated text). One button uses `ActionKind.CopyToClipboard` with `CopyToClipboardActionOptions.Text`, another uses `ActionKind.Share` with `ShareActionOptions`. The whole strip sits over a `backdrop-blur-md` container so it stays legible against arbitrary content.

## When to use

Any view with a long block of generated/derived text the user might want to share or paste elsewhere — transcripts, summaries, AI explanations, error reports. The floating placement keeps the controls close to the content but out of the reading flow. Don't use for editable fields — the actions belong with the saved/finalized state.

## Snippet

```csharp
private void RenderTranscriptContent(UIView view)
{
    var transcriptText = GetTranscriptAsText();
    var hasContent = _recognizedSpeech.Value.Count > 0;

    view.Box(["h-full overflow-hidden relative"], content: box =>
    {
        box.ScrollArea(
            autoScroll: true,
            autoScrollKey: $"transcript-{_recognizedSpeechVersion.Value}",
            rootStyle: ["h-full"],
            content: scroll =>
            {
                scroll.Column(["p-4 pb-14 gap-3"], content: msgs =>
                {
                    foreach (var entry in _recognizedSpeech.Value)
                    {
                        msgs.Column(["gap-0.5"], content: col =>
                        {
                            col.Row(["gap-2 items-baseline"], content: row =>
                            {
                                row.Text([Text.Label], entry.ParticipantName);
                                row.Text([Text.Caption], FormatTimeInClientTimezone(entry.Timestamp));
                            });
                            col.Text([Text.Body], entry.Text);
                        });
                    }
                });
            });

        if (hasContent)
        {
            box.Row(["absolute bottom-2 right-4 gap-1 rounded-lg backdrop-blur-md p-1"], content: actions =>
            {
                actions.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.ActionButton(
                        ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                        action: ActionKind.CopyToClipboard,
                        options: new CopyToClipboardActionOptions { Text = transcriptText },
                        content: vv => vv.Icon([Icon.Sm], name: "clipboard-copy")),
                    contentSlot: v => v.Text([Text.Caption], "Copy transcript"));

                actions.Tooltip(
                    contentStyle: [Tooltip.Content],
                    trigger: v => v.ActionButton(
                        ["h-8 w-8 inline-flex items-center justify-center rounded-md bg-transparent hover:bg-foreground/10 transition-colors"],
                        action: ActionKind.Share,
                        options: new ShareActionOptions { Title = "Transcript", Text = transcriptText },
                        content: vv => vv.Icon([Icon.Sm], name: "share")),
                    contentSlot: v => v.Text([Text.Caption], "Share transcript"));
            });
        }
    });
}
```

## Notes

- Use `ActionButton` (not `Button`) for `CopyToClipboard` and `Share` — these are platform actions the SDK runs client-side without a server round-trip. A regular `Button` with an `onClick` would have to ship the text to the server and back.
- The container is `relative` and the action strip is `absolute bottom-2 right-4` — pad the scrolled content with `pb-14` so the last lines don't sit underneath the strip.
- `backdrop-blur-md` is the magic ingredient — without it the icons get illegible over varied content. Combine with `bg-transparent` on the buttons themselves.
- Gate the strip on `hasContent` — empty-state placeholders shouldn't show "share nothing".

## See also

- `toast-notifications` — a confirmation toast on copy is a nice optional add
- `expandable-detail-card` — when copy/share affordances belong on each item, not the whole panel
