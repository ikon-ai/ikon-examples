<!-- mined-from: QTribunal -->
# Role-Tagged Transcript Feed — Per-Speaker Style From One Enum

A `TranscriptEntry(Role, Speaker, Text)` record drives a scroll-area feed. The render method maps `Role` to a tuple of (entry-style, speaker-style, text-style) so each speaker class — Q, Player, Narrator, Witness, System — gets its own color, indent, and motion treatment. The entries live in a `ReactiveList<TranscriptEntry>`, so an append re-renders the feed and moves `autoScrollKey`, scrolling to bottom.

## When to use

Multi-voice dialogues — interactive fiction, courtroom games, multi-agent debates, coaching apps, chatbot transcripts where different roles (system / human / agent / tool) need visual separation. Sharper signal than a chat-bubble pattern when there are 4+ distinct speaker classes.

## Snippet

```csharp
public enum TranscriptRole { Q, Player, Narrator, Witness, System }
public record TranscriptEntry(TranscriptRole Role, string Speaker, string Text);

private readonly ReactiveList<TranscriptEntry> _transcript = new();

private void AddTranscript(TranscriptRole role, string speaker, string text)
{
    _transcript.Add(new TranscriptEntry(role, speaker, text));
}

private void RenderTranscript(UIView view)
{
    view.Box(style: [Styles.Transcript.Container], content: view =>
    {
        view.ScrollArea(
            autoScroll: true,
            autoScrollKey: _transcript,
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

- `ReactiveList<T>` is the whole state story: `_transcript.Add(entry)` notifies on its own, and enumerating or reading `Count` during render subscribes. No version counter, no read-for-subscription line.
- Tuple destructuring in the switch keeps the role-to-style mapping in one place. Adding a new role = one tuple line, not three switch statements.
- Optional per-role motion (`QTextMotion`) lets the antagonist's lines fade in dramatically while routine narrator text just appears.
- Pass `autoScrollKey: _transcript` (the reactive list itself — its change version moves on every mutation) so the `ScrollArea` re-anchors on every append, not just on first render. A count (`_transcript.Count`) or a composite string works too; no `.ToString()` needed.

## See also

- `chat-with-tool-calls` — for assistant/tool chats with structured tool-call envelopes
- `chatbot-streaming` — when entries stream token-by-token rather than appearing whole
