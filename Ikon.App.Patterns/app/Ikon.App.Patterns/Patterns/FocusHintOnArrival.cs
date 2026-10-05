namespace Ikon.App.Patterns.Patterns;

// Pattern: focus-hint-on-arrival — see docs/patterns/focus-hint-on-arrival.md.
// The example region below is the canonical body the doc extracts. The demo schedules an alert to
// arrive a few seconds after the press, so it lands while the user is not acting, which is the case
// the live region exists for.
internal sealed class FocusHintOnArrival : IPatternDemo
{
    public string Slug => "focus-hint-on-arrival";
    public string Title => "Announcing what just arrived";
    public string Category => "Status & feedback";

    private const int ArrivalDelayMs = 3000;

    private static readonly (string Text, bool Urgent)[] DemoAlerts =
    [
        ("Backup of the customer database finished (42 GB)", false),
        ("Payment provider is returning errors for 12% of checkouts", true),
        ("Maria Lind commented on the Q3 budget draft", false),
        ("Disk on build-agent-03 is 97% full", true),
        ("Nightly price sync imported 1,284 products", false),
    ];

    private readonly ClientReactive<int> _nextDemoAlert = new(0);

    public void RenderDemo(IView view)
    {
        view.Column(["gap-3 max-w-xl"], content: col =>
        {
            col.Button([Button.OutlineSm, "self-start"], text: $"Send an alert in {ArrivalDelayMs / 1000} s",
                onClick: async () =>
                {
                    var clientSessionId = ReactiveScope.ClientId;
                    var index = _nextDemoAlert.Value;
                    _nextDemoAlert.Value = index + 1;
                    _ = DeliverLaterAsync(clientSessionId, index);
                });
            col.Text(["text-xs text-zinc-400"], "Alerts arrive on their own; a screen reader announces urgent ones at once and queues the rest");
            Render(col);
        });
    }

    private sealed record Alert(string Id, string Text, bool Urgent);

    private async Task DeliverLaterAsync(int clientSessionId, int index)
    {
        await Task.Delay(ArrivalDelayMs);
        var (text, urgent) = DemoAlerts[index % DemoAlerts.Length];
        _alerts.AddFor(clientSessionId, new Alert($"alert-{index}", text, urgent));
    }

    #region example:pattern-focus-hint-on-arrival
    private readonly ClientReactiveList<Alert> _alerts = new();

    private void Render(IView view)
    {
        view.Column(["gap-2"], content: col =>
        {
            foreach (var alert in _alerts)
            {
                col.Column(["gap-1"], key: alert.Id, content: item =>
                {
                    item.Text(text: alert.Text);

                    // FocusHint maps to an ARIA live region. Content that appears without the
                    // user acting for it is invisible to a screen reader otherwise -- they are
                    // not looking at the part of the page that changed.
                    item.FocusHint(new FocusHintProps
                    {
                        // Assertive INTERRUPTS whatever is being read. Reserve it for something
                        // that cannot wait; Polite queues behind the current utterance and is
                        // right for almost everything.
                        Priority = alert.Urgent ? FocusPriority.Assertive : FocusPriority.Polite,

                        // Ranking settles competing hints when several arrive together: only the
                        // highest-ranked is announced, rather than the last to render.
                        Ranking = alert.Urgent ? 100 : 0,

                        // Cooldown suppresses re-announcing the same region while it churns --
                        // without it a list that updates every second talks continuously.
                        Cooldown = TimeSpan.FromSeconds(2),

                        // FocusOnly moves focus without announcing, for when the visible change
                        // already says what happened.
                        FocusOnly = false,
                    });
                });
            }
        });
    }
    #endregion
}
