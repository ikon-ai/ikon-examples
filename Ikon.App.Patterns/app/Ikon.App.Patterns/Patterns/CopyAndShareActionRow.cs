namespace Ikon.App.Patterns.Patterns;

// Pattern: copy-and-share-action-row — see docs/patterns/copy-and-share-action-row.md.
// The speech record, reactive transcript list, and formatting helpers below stand in for the app's
// real read-only content the floating actions copy and share.
internal sealed class CopyAndShareActionRow : IPatternDemo
{
    public string Slug => "copy-and-share-action-row";
    public string Title => "Copy and share action row";
    public string Category => "Interaction";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "A floating row of declarative copy-to-clipboard and share ActionButtons over read-only content. See the source and docs/patterns/copy-and-share-action-row.md.");

    private sealed record SpeechEntry(string ParticipantName, DateTime Timestamp, string Text);

    private readonly ReactiveList<SpeechEntry> _recognizedSpeech = new();
    private readonly Reactive<int> _recognizedSpeechVersion = new(0);

    private string GetTranscriptAsText() => throw new NotImplementedException();

    private string FormatTimeInClientTimezone(DateTime timestamp) => throw new NotImplementedException();

    #region docsnippet:pattern-copy-and-share-action-row
    private void RenderTranscriptContent(UIView view)
    {
        var transcriptText = GetTranscriptAsText();
        var hasContent = _recognizedSpeech.Count > 0;

        view.Box(["h-full overflow-hidden relative"], content: box =>
        {
            box.ScrollArea(
                autoScroll: true,
                autoScrollKey: _recognizedSpeechVersion.Value,
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
    #endregion
}
