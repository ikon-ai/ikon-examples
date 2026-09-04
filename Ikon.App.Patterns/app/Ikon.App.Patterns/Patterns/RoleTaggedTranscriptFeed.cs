namespace Ikon.App.Patterns.Patterns;

// Pattern: role-tagged-transcript-feed — see docs/patterns/role-tagged-transcript-feed.md.
// The docsnippet region maps one role enum to per-speaker styles; the Styles stub outside it stands in
// for the app's committed style tokens the render reads.
internal sealed class RoleTaggedTranscriptFeed : IPatternDemo
{
    public string Slug => "role-tagged-transcript-feed";
    public string Title => "Role-tagged transcript feed";
    public string Category => "Chat";
    public void RenderDemo(IView view) => RenderTranscript(view);

    private static class Styles
    {
        public static class Transcript
        {
            public const string Container = "flex-1 min-h-0";
            public const string EntryBase = "px-3 py-2 rounded-md";
            public const string EntryMotion = "motion-opacity-in-0";
            public const string QEntry = "bg-rose-500/10";
            public const string QSpeaker = "text-xs font-semibold text-rose-400";
            public const string QText = "text-sm text-foreground";
            public const string QTextMotion = "motion-blur-in-sm";
            public const string PlayerEntry = "bg-sky-500/10";
            public const string PlayerSpeaker = "text-xs font-semibold text-sky-400";
            public const string PlayerText = "text-sm text-foreground";
            public const string NarratorEntry = "bg-muted/40";
            public const string NarratorSpeaker = "text-xs font-semibold text-muted-foreground";
            public const string NarratorText = "text-sm italic text-muted-foreground";
            public const string WitnessEntry = "bg-amber-500/10";
            public const string WitnessSpeaker = "text-xs font-semibold text-amber-400";
            public const string WitnessText = "text-sm text-foreground";
            public const string SystemEntry = "bg-transparent";
            public const string SystemSpeaker = "text-xs font-semibold text-muted-foreground";
            public const string SystemText = "text-xs text-muted-foreground";
        }
    }

    #region docsnippet:pattern-role-tagged-transcript-feed
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
    #endregion
}
