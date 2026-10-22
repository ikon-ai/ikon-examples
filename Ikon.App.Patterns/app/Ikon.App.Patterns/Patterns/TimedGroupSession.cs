namespace Ikon.App.Patterns.Patterns;

// Pattern: timed-group-session — see docs/patterns/timed-group-session.md.
// In the gallery the guide's lines are spoken by the speech mock; the bells are real sound.
internal sealed class TimedGroupSession(IAppBase app) : IPatternDemo
{
    public string Slug => "timed-group-session";
    public string Title => "Timed group session with a shared bell";
    public string Category => "Voice & audio";

    public void RenderDemo(IView view) => RenderSession((UIView)view);

    #region example:pattern-timed-group-session
    private enum Phase { Lobby, Running, Ended }

    private Audio Audio { get; } = new(app);

    // One instance for everyone (SessionIdentity()), so this state is the room's
    private readonly Reactive<Phase> _phase = new(Phase.Lobby);
    private readonly Reactive<int> _secondsLeft = new(0);
    private readonly ReactiveHashSet<int> _joined = new();
    private CancellationTokenSource? _session;

    // A soft bell made in code: a fading 528 Hz tone with a quieter overtone, so no file is needed
    private static readonly AudioClip Bell = MakeBell();

    /// <summary>Call once from Main.</summary>
    private void WireSession()
    {
        app.OnClientLeft(async ctx => _joined.Remove(ctx.SessionId));
    }

    private void Start(TimeSpan length)
    {
        if (_phase.Value == Phase.Running)
        {
            return;
        }

        _session = new CancellationTokenSource();
        _ = Task.Run(() => RunSessionAsync(length, _session.Token));
    }

    private async Task RunSessionAsync(TimeSpan length, CancellationToken ct)
    {
        // Each line is spoken once, at its offset from the start
        var script = new Queue<(TimeSpan At, string Text)>(
        [
            (TimeSpan.FromSeconds(3), "Welcome. Sit comfortably, and let your eyes close."),
            (length / 2, "Halfway. If your mind has wandered, come back to the breath."),
            (length - TimeSpan.FromSeconds(20), "In a moment the bell will sound. Take one more slow breath."),
        ]);

        var startedAt = DateTimeOffset.UtcNow;
        _phase.Value = Phase.Running;
        Audio.Play(Listeners(), Bell);

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            do
            {
                // From the clock, not a count of ticks, so a late tick does not stretch the session
                var elapsed = DateTimeOffset.UtcNow - startedAt;
                _secondsLeft.Value = Math.Max(0, (int)Math.Ceiling((length - elapsed).TotalSeconds));

                while (script.TryPeek(out var line) && line.At <= elapsed)
                {
                    script.Dequeue();
                    Audio.Speak(Listeners(), line.Text);
                }
            }
            while (_secondsLeft.Value > 0 && await timer.WaitForNextTickAsync(ct));

            Audio.Play(Listeners(), Bell);
            _phase.Value = Phase.Ended;
        }
        catch (OperationCanceledException)
        {
            Audio.Stop(MediaTargets.Everyone);
            _phase.Value = Phase.Lobby;
        }
    }

    // Read at each line, so someone who joins halfway hears the rest
    private MediaTargets Listeners() => MediaTargets.To([.. _joined.Peek]);

    private void RenderSession(UIView view)
    {
        view.Column([Card.Default, "gap-4 p-6 w-full max-w-md items-center"], content: col =>
        {
            var clientSessionId = ReactiveScope.ClientId;
            var joined = _joined.Contains(clientSessionId);

            col.Text([Text.Caption], $"{Plural.Of(_joined.Count, "person", "people")} in the session");

            switch (_phase.Value)
            {
                case Phase.Running:
                {
                    col.Text(["text-5xl font-semibold tabular-nums"], $"{_secondsLeft.Value / 60}:{_secondsLeft.Value % 60:00}");
                    col.Button([Button.GhostSm], text: "End early", onClick: async () => _session?.Cancel());
                    break;
                }
                case Phase.Ended:
                {
                    col.Text([Text.H3], "Session complete");
                    col.Button([Button.OutlineMd], text: "Back to the lobby", onClick: async () => _phase.Value = Phase.Lobby);
                    break;
                }
                default:
                {
                    col.Text([Text.Body], "Join, then start when everyone is here.");
                    col.Row(["gap-2"], content: row =>
                    {
                        row.Button([Button.OutlineMd], text: "1 minute", onClick: async () => Start(TimeSpan.FromMinutes(1)));
                        row.Button([Button.OutlineMd], text: "5 minutes", onClick: async () => Start(TimeSpan.FromMinutes(5)));
                        row.Button([Button.OutlineMd], text: "10 minutes", onClick: async () => Start(TimeSpan.FromMinutes(10)));
                    });
                    break;
                }
            }

            // Only people who joined hear the bell and the voice, not everyone with the page open
            col.Button(joined ? [Button.GhostMd] : [Button.PrimaryMd], text: joined ? "Leave the session" : "Join the session", onClick: async () =>
            {
                if (joined)
                {
                    _joined.Remove(clientSessionId);
                }
                else
                {
                    _joined.Add(clientSessionId);
                }
            });
        });
    }

    private static AudioClip MakeBell()
    {
        const int sampleRate = 48000;
        var samples = new float[sampleRate * 5];

        for (var i = 0; i < samples.Length; i++)
        {
            var t = (double)i / sampleRate;
            samples[i] = (float)(Math.Exp(-1.1 * t) * (0.45 * Math.Sin(2 * Math.PI * 528 * t) + 0.15 * Math.Sin(2 * Math.PI * 1320 * t)));
        }

        return new AudioClip(samples, sampleRate);
    }
    #endregion
}
