namespace Ikon.App.Patterns.Patterns;

// Pattern: focus-hint-on-arrival — see docs/patterns/focus-hint-on-arrival.md.
// The docsnippet region below is the canonical body the doc extracts.
internal sealed class FocusHintOnArrival : IPatternDemo
{
    public string Slug => "focus-hint-on-arrival";
    public string Title => "Announcing what just arrived";
    public string Category => "Status & feedback";
    public void RenderDemo(IView view) => Render(view);

    private sealed record Alert(string Id, string Text, bool Urgent);

    #region docsnippet:pattern-focus-hint-on-arrival
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

                        // Ranking orders competing hints when several arrive together, so the
                        // most important one is announced first rather than the last to render.
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
