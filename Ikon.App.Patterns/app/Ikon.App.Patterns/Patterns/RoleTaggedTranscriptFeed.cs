namespace Ikon.App.Patterns.Patterns;

// Pattern: role-tagged-transcript-feed — see docs/patterns/role-tagged-transcript-feed.md.
// The example region maps one role enum to per-speaker styles; the Styles stub outside it stands in
// for the app's committed style tokens the render reads. The demo seeds an interrogation scene and
// appends the rest of its script one line per press, so the per-role styling and the auto-scroll show.
internal sealed class RoleTaggedTranscriptFeed : IPatternDemo
{
    public string Slug => "role-tagged-transcript-feed";
    public string Title => "Role-tagged transcript feed";
    public string Category => "Chat";

    private static readonly (TranscriptRole Role, string Speaker, string Text)[] DemoScript =
    [
        (TranscriptRole.System, "System", "Case 14 · The Lighthouse Keeper · interview started"),
        (TranscriptRole.Narrator, "Narrator", "Rain hammers the window of the harbour office. Mrs. Ahlberg keeps her coat on."),
        (TranscriptRole.Q, "Q", "Where were you when the lamp went dark on Tuesday night?"),
        (TranscriptRole.Witness, "Mrs. Ahlberg", "At home. I saw the beam stop from my kitchen, a little after eleven."),
        (TranscriptRole.Player, "You", "Did anyone else see it go out?"),
        (TranscriptRole.Witness, "Mrs. Ahlberg", "The ferryman, Olsen. He was still tying up his boat."),
        (TranscriptRole.Narrator, "Narrator", "She glances at the door, then back at her hands."),
        (TranscriptRole.Q, "Q", "Olsen says the harbour was empty by ten. One of you is wrong."),
        (TranscriptRole.Player, "You", "Show her the logbook page."),
        (TranscriptRole.System, "System", "Evidence presented: Logbook, page 212"),
        (TranscriptRole.Witness, "Mrs. Ahlberg", "That is not my husband's handwriting."),
    ];

    private const int DemoSeedCount = 5;

    public RoleTaggedTranscriptFeed() => SeedScene();

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-2xl"], content: col =>
        {
            col.Row(["gap-2"], content: row =>
            {
                var next = _transcript.Count;
                row.Button([Button.OutlineSm], text: next < DemoScript.Length ? "Next line" : "Scene over",
                    disabled: next >= DemoScript.Length,
                    onClick: async () =>
                    {
                        if (_transcript.Count < DemoScript.Length)
                        {
                            var (role, speaker, text) = DemoScript[_transcript.Count];
                            AddTranscript(role, speaker, text);
                        }
                    });
                row.Button([Button.GhostSm], text: "Restart scene",
                    onClick: async () =>
                    {
                        _transcript.Clear();
                        SeedScene();
                    });
            });

            col.Column(["h-[360px] rounded-lg border border-white/10 p-2"], content: frame => RenderTranscript(frame));
        });
    }

    private void SeedScene()
    {
        foreach (var (role, speaker, text) in DemoScript.Take(DemoSeedCount))
        {
            AddTranscript(role, speaker, text);
        }
    }

    private static class Styles
    {
        public static class Transcript
        {
            public const string Container = "flex-1 min-h-0";
            public const string EntryBase = "flex flex-col gap-0.5 px-3 py-2 mb-2 rounded-md";
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

    #region example:pattern-role-tagged-transcript-feed
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
