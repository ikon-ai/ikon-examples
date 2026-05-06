<!-- mined-from: QTribunal -->
# Role-Tagged Transcript Feed — Per-Speaker Style From One Enum

A `TranscriptEntry(Role, Speaker, Text)` record drives a scroll-area feed. The render method maps `Role` to a tuple of (entry-style, speaker-style, text-style) so each speaker class — Q, Player, Narrator, Witness, System — gets its own color, indent, and motion treatment. New entries bump a `_transcriptVersion` counter that the `ScrollArea`'s `autoScrollKey` watches, so the feed scrolls to bottom on every append.

## When to use

Multi-voice dialogues — interactive fiction, courtroom games, multi-agent debates, coaching apps, chatbot transcripts where different roles (system / human / agent / tool) need visual separation. Sharper signal than a chat-bubble pattern when there are 4+ distinct speaker classes.

## Snippet

```csharp
public enum TranscriptRole { Q, Player, Narrator, Witness, System }
public record TranscriptEntry(TranscriptRole Role, string Speaker, string Text);

private readonly List<TranscriptEntry> _transcript = [];
private readonly Reactive<int> _transcriptVersion = new(0);

private void AddTranscript(TranscriptRole role, string speaker, string text)
{
    _transcript.Add(new TranscriptEntry(role, speaker, text));
    _transcriptVersion.Value++;
}

private void RenderTranscript(UIView view)
{
    _ = _transcriptVersion.Value;

    view.Box(style: [Styles.Transcript.Container], content: view =>
    {
        view.ScrollArea(
            autoScroll: true,
            autoScrollKey: _transcriptVersion.Value.ToString(),
            rootStyle: [ScrollArea.Root, "h-full"],
            content: scrollView =>
            {
                foreach (var entry in _transcript)
                {
                    RenderTranscriptEntry(scrollView, entry);
                }
            });
    });
}

private void RenderTranscriptEntry(UIView view, TranscriptEntry entry)
{
    var (entryStyle, speakerStyle, textStyle) = entry.Role switch
    {
        TranscriptRole.Q       => (Styles.Transcript.QEntry, Styles.Transcript.QSpeaker, Styles.Transcript.QText),
        TranscriptRole.Player  => (Styles.Transcript.PlayerEntry, Styles.Transcript.PlayerSpeaker, Styles.Transcript.PlayerText),
        TranscriptRole.Narrator => (Styles.Transcript.NarratorEntry, Styles.Transcript.NarratorSpeaker, Styles.Transcript.NarratorText),
        TranscriptRole.Witness => (Styles.Transcript.WitnessEntry, Styles.Transcript.WitnessSpeaker, Styles.Transcript.WitnessText),
        _ => (Styles.Transcript.SystemEntry, Styles.Transcript.SystemSpeaker, Styles.Transcript.SystemText)
    };

    var textMotion = entry.Role == TranscriptRole.Q ? Styles.Transcript.QTextMotion : "";

    view.Box(style: [Styles.Transcript.EntryBase, entryStyle, Styles.Transcript.EntryMotion], content: view =>
    {
        view.Text(style: [speakerStyle], text: entry.Speaker);
        view.Text(style: [textStyle, textMotion], text: entry.Text);
    });
}
```

## Notes

- `_ = _transcriptVersion.Value;` at the top of `RenderTranscript` is a deliberate read-for-subscription — without it, mutating `_transcript` (a plain `List<>`) won't re-render. Prefer this over making the list itself reactive when entries are append-only.
- Tuple destructuring in the switch keeps the role-to-style mapping in one place. Adding a new role = one tuple line, not three switch statements.
- Optional per-role motion (`QTextMotion`) lets the antagonist's lines fade in dramatically while routine narrator text just appears.
- Pass `autoScrollKey: _transcriptVersion.Value.ToString()` so the `ScrollArea` re-anchors on every append, not just on first render.

## See also

- `chat-with-tool-calls` — for assistant/tool chats with structured tool-call envelopes
- `chatbot-streaming` — when entries stream token-by-token rather than appearing whole
